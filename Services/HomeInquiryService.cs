using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web;
using GLC_EXPRESS.Models;
using Newtonsoft.Json;

namespace GLC_EXPRESS.Services
{
    public static class HomeInquiryService
    {
        private const string StorageFilePath = "~/App_Data/home-inquiries.json";
        private const int MaxAttachmentBytes = 5 * 1024 * 1024;
        private const string DefaultInquirySource = "Сайт";
        private const string DefaultInquiryStatus = "Новая";
        private const string DefaultCommentAuthor = "CRM";
        private static readonly object SyncRoot = new object();
        private static readonly string[] DefaultInquiryStatuses = { "Новая", "Связались", "Ожидаем документы", "В работе", "Оплачено", "Положительно закрыта", "Закрыто", "Отменено" };

        public static HomeInquiryRecord Save(
            HttpServerUtility server,
            string name,
            string email,
            string phone,
            string messenger,
            string direction,
            string cargoType,
            string clientComment,
            HttpPostedFile postedFile)
        {
            if (server == null)
            {
                throw new InvalidOperationException("Сервер недоступен для сохранения заявки.");
            }

            var normalizedName = (name ?? string.Empty).Trim();
            var normalizedEmail = NormalizeEmail(email);

            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                throw new InvalidOperationException("Укажите имя.");
            }

            var inquiry = new HomeInquiryRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = normalizedName,
                Email = normalizedEmail,
                Phone = NormalizeOptionalText(phone),
                Messenger = NormalizeOptionalText(messenger),
                Direction = NormalizeOptionalText(direction),
                CargoType = NormalizeOptionalText(cargoType),
                ClientComment = NormalizeOptionalText(clientComment),
                Source = DefaultInquirySource,
                Status = DefaultInquiryStatus,
                AssignedManager = string.Empty,
                Comments = new List<HomeInquiryCommentRecord>(),
                AttachmentPath = SaveAttachment(server, normalizedName, postedFile),
                CreatedAtUtc = DateTime.UtcNow
            };

            lock (SyncRoot)
            {
                var physicalPath = server.MapPath(StorageFilePath);
                var folderPath = Path.GetDirectoryName(physicalPath);

                if (!string.IsNullOrWhiteSpace(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var inquiries = LoadExisting(physicalPath);
                inquiries.Insert(0, inquiry);
                File.WriteAllText(physicalPath, JsonConvert.SerializeObject(inquiries, Formatting.Indented));

                using (var database = AppDatabase.Open())
                {
                    database.GetCollection<HomeInquiryRecord>("home_inquiries").Upsert(inquiry);
                }
            }

            return inquiry;
        }

        public static IReadOnlyList<string> GetAvailableStatuses()
        {
            return DefaultInquiryStatuses;
        }

        public static bool HasNewInquiries()
        {
            try
            {
                using (var database = AppDatabase.Open())
                {
                    return database.GetCollection<HomeInquiryRecord>("home_inquiries")
                        .FindAll()
                        .Any(item => item != null && string.Equals(NormalizeStatus(item.Status), DefaultInquiryStatus, StringComparison.OrdinalIgnoreCase));
                }
            }
            catch
            {
            }

            try
            {
                var storagePath = HttpContext.Current == null || HttpContext.Current.Server == null
                    ? string.Empty
                    : HttpContext.Current.Server.MapPath(StorageFilePath);

                if (string.IsNullOrWhiteSpace(storagePath) || !File.Exists(storagePath))
                {
                    return false;
                }

                return LoadExisting(storagePath)
                    .Any(item => item != null && string.Equals(NormalizeStatus(item.Status), DefaultInquiryStatus, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        public static HomeInquiryRecord UpdateManagementFields(string inquiryId, string source, string status, string assignedManager)
        {
            var normalizedId = (inquiryId ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedId))
            {
                throw new InvalidOperationException("Не удалось определить заявку для обновления.");
            }

            HomeInquiryRecord updatedInquiry = null;

            lock (SyncRoot)
            {
                using (var database = AppDatabase.Open())
                {
                    var collection = database.GetCollection<HomeInquiryRecord>("home_inquiries");
                    var existingInquiry = collection.FindById(normalizedId);

                    if (existingInquiry == null)
                    {
                        throw new InvalidOperationException("Заявка не найдена.");
                    }

                    existingInquiry.Source = NormalizeSource(source);
                    existingInquiry.Status = NormalizeStatus(status);
                    existingInquiry.AssignedManager = NormalizeAssignedManager(assignedManager);
                    existingInquiry.Comments = NormalizeComments(existingInquiry.Comments);
                    collection.Update(existingInquiry);
                    updatedInquiry = existingInquiry;
                }

                PersistInquiryToJson(updatedInquiry);
            }

            return updatedInquiry;
        }

        public static HomeInquiryRecord AddComment(string inquiryId, string author, string text)
        {
            var normalizedId = (inquiryId ?? string.Empty).Trim();
            var normalizedText = NormalizeCommentText(text);

            if (string.IsNullOrWhiteSpace(normalizedId))
            {
                throw new InvalidOperationException("Не удалось определить заявку для комментария.");
            }

            if (string.IsNullOrWhiteSpace(normalizedText))
            {
                throw new InvalidOperationException("Введите комментарий перед сохранением.");
            }

            HomeInquiryRecord updatedInquiry = null;

            lock (SyncRoot)
            {
                using (var database = AppDatabase.Open())
                {
                    var collection = database.GetCollection<HomeInquiryRecord>("home_inquiries");
                    var existingInquiry = collection.FindById(normalizedId);

                    if (existingInquiry == null)
                    {
                        throw new InvalidOperationException("Заявка не найдена.");
                    }

                    existingInquiry.Comments = NormalizeComments(existingInquiry.Comments);
                    existingInquiry.Comments.Insert(0, new HomeInquiryCommentRecord
                    {
                        Author = NormalizeCommentAuthor(author),
                        Text = normalizedText,
                        CreatedAtUtc = DateTime.UtcNow
                    });

                    collection.Update(existingInquiry);
                    updatedInquiry = existingInquiry;
                }

                PersistInquiryToJson(updatedInquiry);
            }

            return updatedInquiry;
        }

        public static List<HomeInquiryRecord> GetRecent(int limit)
        {
            var safeLimit = limit <= 0 ? 20 : limit;

            try
            {
                using (var database = AppDatabase.Open())
                {
                    var items = database.GetCollection<HomeInquiryRecord>("home_inquiries")
                        .FindAll()
                        .OrderByDescending(item => item.CreatedAtUtc)
                        .Take(safeLimit)
                        .ToList();

                    if (items.Count > 0)
                    {
                        return items;
                    }
                }
            }
            catch
            {
            }

            try
            {
                var storagePath = HttpContext.Current == null || HttpContext.Current.Server == null
                    ? string.Empty
                    : HttpContext.Current.Server.MapPath(StorageFilePath);

                if (string.IsNullOrWhiteSpace(storagePath) || !File.Exists(storagePath))
                {
                    return new List<HomeInquiryRecord>();
                }

                return LoadExisting(storagePath)
                    .OrderByDescending(item => item.CreatedAtUtc)
                    .Take(safeLimit)
                    .ToList();
            }
            catch
            {
                return new List<HomeInquiryRecord>();
            }
        }

        private static void PersistInquiryToJson(HomeInquiryRecord inquiry)
        {
            if (inquiry == null || HttpContext.Current == null || HttpContext.Current.Server == null)
            {
                return;
            }

            var physicalPath = HttpContext.Current.Server.MapPath(StorageFilePath);
            var folderPath = Path.GetDirectoryName(physicalPath);

            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var inquiries = LoadExisting(physicalPath);
            var existingIndex = inquiries.FindIndex(item => string.Equals(item.Id, inquiry.Id, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                inquiries[existingIndex] = inquiry;
            }
            else
            {
                inquiries.Insert(0, inquiry);
            }

            File.WriteAllText(physicalPath, JsonConvert.SerializeObject(inquiries, Formatting.Indented));
        }

        private static List<HomeInquiryRecord> LoadExisting(string physicalPath)
        {
            if (!File.Exists(physicalPath))
            {
                return new List<HomeInquiryRecord>();
            }

            var json = File.ReadAllText(physicalPath);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<HomeInquiryRecord>();
            }

            try
            {
                var items = JsonConvert.DeserializeObject<List<HomeInquiryRecord>>(json) ?? new List<HomeInquiryRecord>();

                foreach (var item in items)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    item.Phone = NormalizeOptionalText(item.Phone);
                    item.Messenger = NormalizeOptionalText(item.Messenger);
                    item.Direction = NormalizeOptionalText(item.Direction);
                    item.CargoType = NormalizeOptionalText(item.CargoType);
                    item.ClientComment = NormalizeOptionalText(item.ClientComment);
                    item.Source = NormalizeSource(item.Source);
                    item.Status = NormalizeStatus(item.Status);
                    item.AssignedManager = NormalizeAssignedManager(item.AssignedManager);
                    item.Comments = NormalizeComments(item.Comments);
                }

                return items;
            }
            catch (JsonException)
            {
                return new List<HomeInquiryRecord>();
            }
        }

        private static string SaveAttachment(HttpServerUtility server, string name, HttpPostedFile postedFile)
        {
            if (postedFile == null || postedFile.ContentLength <= 0)
            {
                return string.Empty;
            }

            if (postedFile.ContentLength > MaxAttachmentBytes)
            {
                throw new InvalidOperationException("Размер вложения не должен превышать 5 МБ.");
            }

            var extension = (Path.GetExtension(postedFile.FileName) ?? string.Empty).Trim().ToLowerInvariant();
            var allowedExtensions = CrmSettingsService.GetCurrent().AllowedDocumentExtensions ?? new List<string>();

            if (string.IsNullOrWhiteSpace(extension)
                || !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Для вложения разрешены только файлы: " + string.Join(", ", allowedExtensions));
            }

            var fileName = MakeSafeFileName(name) + "_" + Guid.NewGuid().ToString("N").Substring(0, 8) + extension;
            var storedPath = StoredFileService.BuildProtectedAppRelativePath("HomeInquiries/Attachments", fileName);
            var physicalPath = server.MapPath(storedPath);
            var physicalFolder = Path.GetDirectoryName(physicalPath);

            Directory.CreateDirectory(physicalFolder);

            postedFile.SaveAs(physicalPath);
            return storedPath;
        }

        private static string NormalizeEmail(string email)
        {
            try
            {
                return new MailAddress((email ?? string.Empty).Trim()).Address;
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("Укажите корректный email.");
            }
        }

        private static string MakeSafeFileName(string value)
        {
            var candidate = (value ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(candidate))
            {
                return "inquiry";
            }

            foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            {
                candidate = candidate.Replace(invalidCharacter, '_');
            }

            return candidate.Replace(' ', '_');
        }

        private static string NormalizeSource(string source)
        {
            var normalizedSource = (source ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(normalizedSource) ? DefaultInquirySource : normalizedSource;
        }

        private static string NormalizeStatus(string status)
        {
            var normalizedStatus = (status ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedStatus))
            {
                return DefaultInquiryStatus;
            }

            var matchedStatus = DefaultInquiryStatuses.FirstOrDefault(item => string.Equals(item, normalizedStatus, StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrWhiteSpace(matchedStatus) ? DefaultInquiryStatus : matchedStatus;
        }

        private static string NormalizeAssignedManager(string assignedManager)
        {
            return (assignedManager ?? string.Empty).Trim();
        }

        private static string NormalizeOptionalText(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        private static string NormalizeCommentText(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        private static string NormalizeCommentAuthor(string author)
        {
            var normalizedAuthor = (author ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(normalizedAuthor) ? DefaultCommentAuthor : normalizedAuthor;
        }

        private static List<HomeInquiryCommentRecord> NormalizeComments(IEnumerable<HomeInquiryCommentRecord> comments)
        {
            return (comments ?? Enumerable.Empty<HomeInquiryCommentRecord>())
                .Where(comment => comment != null && !string.IsNullOrWhiteSpace(comment.Text))
                .Select(comment => new HomeInquiryCommentRecord
                {
                    Author = NormalizeCommentAuthor(comment.Author),
                    Text = NormalizeCommentText(comment.Text),
                    CreatedAtUtc = comment.CreatedAtUtc == DateTime.MinValue ? DateTime.UtcNow : comment.CreatedAtUtc
                })
                .ToList();
        }
    }
}