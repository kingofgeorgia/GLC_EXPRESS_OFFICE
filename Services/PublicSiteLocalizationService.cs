using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Web;

namespace GLC_EXPRESS.Services
{
    public static class PublicSiteLocalizationService
    {
        public const string DefaultLanguageCode = "ru";
        public const string LanguageQueryStringKey = "lang";
        private const string LanguageCookieName = "glc-public-lang";
        private const string LanguageItemKey = "glc-public-lang";
        private const string AllowedAttachmentPrefix = "Для вложения разрешены только файлы:";

        private sealed class LocalizedText
        {
            public LocalizedText(string russian, string english, string georgian)
            {
                Russian = russian ?? string.Empty;
                English = english ?? russian ?? string.Empty;
                Georgian = georgian ?? russian ?? string.Empty;
            }

            public string Russian { get; private set; }

            public string English { get; private set; }

            public string Georgian { get; private set; }

            public string GetValue(string languageCode)
            {
                switch (NormalizeLanguageCode(languageCode))
                {
                    case "en":
                        return English;
                    case "ge":
                        return Georgian;
                    default:
                        return Russian;
                }
            }
        }

        private static readonly string[] SupportedLanguageCodes = { "ru", "en", "ge" };
        private static readonly IReadOnlyDictionary<string, LocalizedText> Texts = CreateTexts();

        public static IReadOnlyList<string> GetSupportedLanguageCodes()
        {
            return SupportedLanguageCodes;
        }

        public static void ApplyRequestLanguage(HttpContext context)
        {
            if (context == null)
            {
                return;
            }

            var languageCode = ResolveLanguageCode(context.Request);
            context.Items[LanguageItemKey] = languageCode;
            ApplyThreadCulture(languageCode);

            var queryLanguageCode = NormalizeLanguageCode(context.Request == null ? null : context.Request.QueryString[LanguageQueryStringKey]);
            if (!string.IsNullOrWhiteSpace(queryLanguageCode))
            {
                PersistLanguagePreference(context.Response, queryLanguageCode);
            }
        }

        public static string GetCurrentLanguageCode()
        {
            var context = HttpContext.Current;
            if (context != null)
            {
                var cachedValue = Convert.ToString(context.Items[LanguageItemKey]);
                var normalizedCachedValue = NormalizeLanguageCode(cachedValue);
                if (!string.IsNullOrWhiteSpace(normalizedCachedValue))
                {
                    return normalizedCachedValue;
                }

                var resolved = ResolveLanguageCode(context.Request);
                context.Items[LanguageItemKey] = resolved;
                return resolved;
            }

            return DefaultLanguageCode;
        }

        public static string GetCurrentHtmlLanguage()
        {
            switch (GetCurrentLanguageCode())
            {
                case "ge":
                    return "ka";
                case "en":
                    return "en";
                default:
                    return "ru";
            }
        }

        public static bool IsCurrentLanguage(string languageCode)
        {
            return string.Equals(GetCurrentLanguageCode(), NormalizeLanguageCode(languageCode), StringComparison.OrdinalIgnoreCase);
        }

        public static string GetText(string key)
        {
            return GetText(key, GetCurrentLanguageCode());
        }

        public static string GetText(string key, string languageCode)
        {
            LocalizedText localizedText;
            if (!Texts.TryGetValue(key ?? string.Empty, out localizedText))
            {
                return key ?? string.Empty;
            }

            return localizedText.GetValue(languageCode);
        }

        public static string LocalizeHomeInquiryError(string message)
        {
            var normalizedMessage = (message ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedMessage))
            {
                return string.Empty;
            }

            if (string.Equals(normalizedMessage, "Сервер недоступен для сохранения заявки.", StringComparison.OrdinalIgnoreCase))
            {
                return GetText("InquiryErrorServerUnavailable");
            }

            if (string.Equals(normalizedMessage, "Укажите имя.", StringComparison.OrdinalIgnoreCase))
            {
                return GetText("InquiryErrorNameRequired");
            }

            if (string.Equals(normalizedMessage, "Размер вложения не должен превышать 5 МБ.", StringComparison.OrdinalIgnoreCase))
            {
                return GetText("InquiryErrorAttachmentTooLarge");
            }

            if (string.Equals(normalizedMessage, "Укажите корректный email.", StringComparison.OrdinalIgnoreCase))
            {
                return GetText("InquiryErrorEmailInvalid");
            }

            if (normalizedMessage.StartsWith(AllowedAttachmentPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var allowedExtensions = normalizedMessage.Substring(AllowedAttachmentPrefix.Length).Trim();
                return string.Format(GetText("InquiryErrorAttachmentTypes"), allowedExtensions);
            }

            return normalizedMessage;
        }

        public static string BuildCurrentPageLanguageUrl(HttpRequest request, string languageCode)
        {
            if (request == null)
            {
                return string.Empty;
            }

            var currentUrl = request.RawUrl;
            if (string.IsNullOrWhiteSpace(currentUrl) && request.Url != null)
            {
                currentUrl = request.Url.PathAndQuery;
            }

            return ApplyLanguageToUrl(currentUrl, languageCode);
        }

        public static string ApplyLanguageToUrl(string url)
        {
            return ApplyLanguageToUrl(url, GetCurrentLanguageCode());
        }

        public static string ApplyLanguageToUrl(string url, string languageCode)
        {
            var normalizedLanguageCode = NormalizeLanguageCode(languageCode);
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(normalizedLanguageCode))
            {
                return url ?? string.Empty;
            }

            var anchor = string.Empty;
            var anchorIndex = url.IndexOf('#');
            var urlWithoutAnchor = url;

            if (anchorIndex >= 0)
            {
                anchor = url.Substring(anchorIndex);
                urlWithoutAnchor = url.Substring(0, anchorIndex);
            }

            var queryIndex = urlWithoutAnchor.IndexOf('?');
            var path = queryIndex >= 0 ? urlWithoutAnchor.Substring(0, queryIndex) : urlWithoutAnchor;
            var queryText = queryIndex >= 0 ? urlWithoutAnchor.Substring(queryIndex) : string.Empty;
            var query = HttpUtility.ParseQueryString(queryText ?? string.Empty);

            query.Set(LanguageQueryStringKey, normalizedLanguageCode);

            var serializedQuery = query.ToString();
            return path + (string.IsNullOrWhiteSpace(serializedQuery) ? string.Empty : "?" + serializedQuery) + anchor;
        }

        public static string NormalizeLanguageCode(string languageCode)
        {
            var normalizedValue = (languageCode ?? string.Empty).Trim().ToLowerInvariant();

            if (normalizedValue.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
            {
                return "ru";
            }

            if (normalizedValue.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                return "en";
            }

            if (normalizedValue.StartsWith("ge", StringComparison.OrdinalIgnoreCase) || normalizedValue.StartsWith("ka", StringComparison.OrdinalIgnoreCase))
            {
                return "ge";
            }

            return string.Empty;
        }

        private static string ResolveLanguageCode(HttpRequest request)
        {
            var queryLanguageCode = NormalizeLanguageCode(request == null ? null : request.QueryString[LanguageQueryStringKey]);
            if (!string.IsNullOrWhiteSpace(queryLanguageCode))
            {
                return queryLanguageCode;
            }

            var cookieLanguageCode = NormalizeLanguageCode(request == null || request.Cookies == null || request.Cookies[LanguageCookieName] == null
                ? null
                : request.Cookies[LanguageCookieName].Value);
            if (!string.IsNullOrWhiteSpace(cookieLanguageCode))
            {
                return cookieLanguageCode;
            }

            var headerLanguageCode = GetLanguageFromHeaders(request);
            return string.IsNullOrWhiteSpace(headerLanguageCode) ? DefaultLanguageCode : headerLanguageCode;
        }

        private static string GetLanguageFromHeaders(HttpRequest request)
        {
            if (request == null || request.UserLanguages == null)
            {
                return string.Empty;
            }

            foreach (var language in request.UserLanguages)
            {
                var normalizedLanguage = NormalizeLanguageCode(language);
                if (!string.IsNullOrWhiteSpace(normalizedLanguage))
                {
                    return normalizedLanguage;
                }
            }

            return string.Empty;
        }

        private static void PersistLanguagePreference(HttpResponse response, string languageCode)
        {
            if (response == null)
            {
                return;
            }

            var normalizedLanguageCode = NormalizeLanguageCode(languageCode);
            if (string.IsNullOrWhiteSpace(normalizedLanguageCode))
            {
                return;
            }

            var cookie = new HttpCookie(LanguageCookieName, normalizedLanguageCode)
            {
                Expires = DateTime.UtcNow.AddYears(1),
                Path = VirtualPathUtility.ToAbsolute("~/")
            };

            response.Cookies.Set(cookie);
        }

        private static void ApplyThreadCulture(string languageCode)
        {
            var cultureName = "ru-RU";

            switch (NormalizeLanguageCode(languageCode))
            {
                case "en":
                    cultureName = "en-US";
                    break;
                case "ge":
                    cultureName = "ka-GE";
                    break;
            }

            var culture = CultureInfo.GetCultureInfo(cultureName);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }

        private static IReadOnlyDictionary<string, LocalizedText> CreateTexts()
        {
            var texts = new Dictionary<string, LocalizedText>(StringComparer.OrdinalIgnoreCase);

            texts["NavToggleTitle"] = new LocalizedText("Дополнительные опции", "More options", "დამატებითი პარამეტრები");
            texts["NavHome"] = new LocalizedText("Главная", "Home", "მთავარი");
            texts["NavTurnkeyOrder"] = new LocalizedText("Заказ автомобиля", "Car order", "ავტომობილის შეკვეთა");
            texts["NavDelivery"] = new LocalizedText("Доставка", "Delivery", "მიწოდება");
            texts["NavAuctionCenter"] = new LocalizedText("Аукционный центр", "Auction center", "აუქციონის ცენტრი");
            texts["NavAdvantages"] = new LocalizedText("Наши преимущества", "Why us", "ჩვენი უპირატესობები");
            texts["NavAbout"] = new LocalizedText("О нас", "About us", "ჩვენ შესახებ");
            texts["NavPartners"] = new LocalizedText("Наши партнёры", "Partners", "პარტნიორები");
            texts["NavReviews"] = new LocalizedText("Отзывы", "Reviews", "შეფასებები");
            texts["NavContacts"] = new LocalizedText("Контакты", "Contacts", "კონტაქტები");
            texts["AuthMyAccount"] = new LocalizedText("Мой аккаунт", "My Account", "ჩემი ანგარიში");
            texts["AuthSettings"] = new LocalizedText("Настройки", "Settings", "პარამეტრები");
            texts["AuthLogout"] = new LocalizedText("Выйти", "Logout", "გასვლა");
            texts["AuthSignIn"] = new LocalizedText("Войти", "Sign In", "შესვლა");
            texts["LoginLead"] = new LocalizedText("Введите данные учетной записи, чтобы продолжить работу с CRM.", "Enter your account credentials to continue to CRM.", "შეიყვანეთ თქვენი ანგარიშის მონაცემები CRM-ში გასაგრძელებლად.");
            texts["LoginUsername"] = new LocalizedText("Логин", "Username", "მომხმარებლის სახელი");
            texts["LoginPassword"] = new LocalizedText("Пароль", "Password", "პაროლი");
            texts["LoginRememberMe"] = new LocalizedText("Запомнить меня", "Remember me", "დამიმახსოვრე");
            texts["LoginUsernameRequired"] = new LocalizedText("Укажите логин.", "Username is required.", "მომხმარებლის სახელი სავალდებულოა.");
            texts["LoginPasswordRequired"] = new LocalizedText("Укажите пароль.", "Password is required.", "პაროლი სავალდებულოა.");
            texts["LoginInvalidCredentials"] = new LocalizedText("Неверный логин или пароль.", "Invalid username or password.", "მომხმარებლის სახელი ან პაროლი არასწორია.");
            texts["AccessDeniedTitle"] = new LocalizedText("Доступ запрещен", "Access denied", "წვდომა აკრძალულია");
            texts["AccessDeniedLead"] = new LocalizedText("У вашей учетной записи пока нет прав на работу с CRM.", "Your account does not currently have permission to use the CRM.", "თქვენს ანგარიშს ამ დროისთვის CRM-ზე წვდომის უფლება არ აქვს.");
            texts["AccessDeniedRolesLead"] = new LocalizedText("Сейчас для доступа нужны роли: {0}.", "The following roles are currently required for access: {0}.", "წვდომისთვის ამჟამად საჭიროა შემდეგი როლები: {0}.");
            texts["FooterPrivacyPolicy"] = new LocalizedText("Политика конфиденциальности", "Privacy Policy", "კონფიდენციალურობის პოლიტიკა");
            texts["FooterPoweredBy"] = new LocalizedText("Разработано George Khoms", "Powered by George Khoms", "შემუშავებულია George Khoms-ის მიერ");
            texts["CookieNoticeTitle"] = new LocalizedText("Cookie уведомление", "Cookie notice", "Cookie შეტყობინება");
            texts["CookieNoticeText"] = new LocalizedText("Мы используем cookie для корректной работы сайта, аналитики и сохранения ваших предпочтений.", "We use cookies for the correct operation of the site, analytics, and saving your preferences.", "ჩვენ ვიყენებთ cookie ფაილებს საიტის გამართული მუშაობისთვის, ანალიტიკისთვის და თქვენი არჩევანის დასამახსოვრებლად.");
            texts["CookieAccept"] = new LocalizedText("Принять", "Accept", "დადასტურება");
            texts["CookieDecline"] = new LocalizedText("Отклонить", "Decline", "უარყოფა");
            texts["HomePageTitle"] = new LocalizedText("GLC Express | Аукционы и доставка авто из США", "GLC Express | Car auctions and delivery from the USA", "GLC Express | ავტო აუქციონები და ჩამოტანა აშშ-დან");
            texts["HomeHeroEyebrow"] = new LocalizedText("USA & Canada Auto Auctions", "USA & Canada Auto Auctions", "აშშ და კანადის ავტო აუქციონები");
            texts["HomeHeroTitle"] = new LocalizedText("Покупка, доставка и восстановление автомобилей из США под ключ", "Turnkey purchase, delivery, and restoration of cars from the USA", "აშშ-დან ავტომობილების შეძენა, ჩამოტანა და აღდგენა სრული სერვისით");
            texts["HomeHeroLead"] = new LocalizedText("GLC Express сопровождает клиента на каждом этапе: от аналитики лота и торгов на Copart, Manheim и IAAI до отправки в Поти или Батуми, ремонта и передачи автомобиля владельцу.", "GLC Express supports the client at every stage: from lot analysis and bidding on Copart, Manheim, and IAAI to shipment to Poti or Batumi, repairs, and handover to the owner.", "GLC Express კლიენტს ყველა ეტაპზე ახლავს: Copart, Manheim და IAAI-ზე ლოტის ანალიზიდან და ვაჭრობიდან ფოთში ან ბათუმში გადაზიდვამდე, შეკეთებამდე და მფლობელისთვის გადაცემამდე.");
            texts["HomeHeroPrimaryCta"] = new LocalizedText("Заказ автомобиля", "Order a car", "ავტომობილის შეკვეთა");
            texts["HomeHeroSecondaryCta"] = new LocalizedText("Связаться с менеджером", "Contact a manager", "მენეჯერთან დაკავშირება");
            texts["HomeHeroStatYearsValue"] = new LocalizedText("10 лет", "10 years", "10 წელი");
            texts["HomeHeroStatYearsLabel"] = new LocalizedText("успешной работы с аукционами США и Канады", "of successful work with auctions in the USA and Canada", "აშშ-ისა და კანადის აუქციონებთან წარმატებული მუშაობის");
            texts["HomeHeroStatDeliveryValue"] = new LocalizedText("45-70 дней", "45-70 days", "45-70 დღე");
            texts["HomeHeroStatDeliveryLabel"] = new LocalizedText("средний срок доставки автомобиля в Грузию", "average vehicle delivery time to Georgia", "ავტომობილის საქართველოში მიწოდების საშუალო ვადა");
            texts["HomeHeroStatContainerValue"] = new LocalizedText("4 авто", "4 cars", "4 ავტომობილი");
            texts["HomeHeroStatContainerLabel"] = new LocalizedText("в одном контейнере для оптимальной логистики", "per container for efficient logistics", "ერთ კონტეინერში ოპტიმალური ლოჯისტიკისთვის");
            texts["HomeHeroStatSupportValue"] = new LocalizedText("24/7", "24/7", "24/7");
            texts["HomeHeroStatSupportLabel"] = new LocalizedText("поддержка и online-отслеживание на каждом этапе", "support and online tracking at every stage", "მხარდაჭერა და ონლაინ თვალთვალი ყველა ეტაპზე");
            texts["HomeHeroPanelTitle"] = new LocalizedText("Полный цикл для клиента и инвестора", "A full cycle for clients and investors", "სრული ციკლი კლიენტისა და ინვესტორისთვის");
            texts["HomeHeroCheckBudget"] = new LocalizedText("Подбор автомобиля под бюджет, задачу и рынок назначения", "Vehicle selection for budget, task, and target market", "ავტომობილის შერჩევა ბიუჯეტის, ამოცანისა და მიზნობრივი ბაზრის მიხედვით");
            texts["HomeHeroCheckHistory"] = new LocalizedText("Проверка юридической чистоты, состояния и истории ТС", "Verification of legal status, condition, and vehicle history", "იურიდიული სისუფთავის, მდგომარეობისა და ისტორიის შემოწმება");
            texts["HomeHeroCheckBidding"] = new LocalizedText("Участие в торгах с возможностью наблюдать ставки online", "Participation in auctions with the ability to track bids online", "აუქციონში მონაწილეობა ონლაინ შეთავაზებების დაკვირვების შესაძლებლობით");
            texts["HomeHeroCheckShipping"] = new LocalizedText("Доставка из США, оформление документов и страхование", "Shipping from the USA, paperwork, and insurance", "აშშ-დან გადაზიდვა, დოკუმენტების გაფორმება და დაზღვევა");
            texts["HomeHeroCheckRepair"] = new LocalizedText("Восстановление, русификация и подготовка к реэкспорту", "Restoration, localization, and preparation for re-export", "აღდგენა, ლოკალიზაცია და რეექსპორტისთვის მომზადება");
            texts["HomeHeroRouteLabel"] = new LocalizedText("Маршрут", "Route", "მარშრუტი");
            texts["HomeHeroRouteTitle"] = new LocalizedText("USA -> Поти / Батуми -> Грузия и страны СНГ", "USA -> Poti / Batumi -> Georgia and CIS countries", "აშშ -> ფოთი / ბათუმი -> საქართველო და დსთ-ის ქვეყნები");
            texts["HomeHeroRouteDescription"] = new LocalizedText("Работаем для клиентов из Грузии, России, Казахстана, Армении, Киргизии и других стран региона.", "We work for clients from Georgia, Russia, Kazakhstan, Armenia, Kyrgyzstan, and other countries of the region.", "ვმუშაობთ საქართველოს, რუსეთის, ყაზახეთის, სომხეთის, ყირგიზეთის და რეგიონის სხვა ქვეყნების კლიენტებისთვის.");
            texts["AboutKicker"] = new LocalizedText("О нас", "About us", "ჩვენ შესახებ");
            texts["AboutTitle"] = new LocalizedText("Брокерское и логистическое сопровождение на автоаукционах США и Канады", "Brokerage and logistics support at auto auctions in the USA and Canada", "საბროკერო და ლოჯისტიკური მხარდაჭერა აშშ-ისა და კანადის ავტო აუქციონებზე");
            texts["AboutLead"] = new LocalizedText("GLC Express помогает клиентам покупать автомобили на ведущих американских площадках, готовит документы, ведет логистику и берет на себя послепродажное восстановление. Мы выстраиваем прозрачную цепочку сделки и делаем ее понятной для частных заказчиков, дилеров и партнеров.", "GLC Express helps clients buy vehicles on leading American marketplaces, prepares documents, manages logistics, and handles post-purchase restoration. We build a transparent deal chain and make it clear for private buyers, dealers, and partners.", "GLC Express კლიენტებს ეხმარება ავტომობილების შეძენაში წამყვან ამერიკულ პლატფორმებზე, ამზადებს დოკუმენტებს, მართავს ლოჯისტიკას და უზრუნველყოფს შემდგომ აღდგენას. ჩვენ ვქმნით გამჭვირვალე გარიგების ჯაჭვს კერძო კლიენტებისთვის, დილერებისთვის და პარტნიორებისთვის.");
            texts["AboutFeatureAnalyticsTitle"] = new LocalizedText("Аукционы и аналитика", "Auctions and analytics", "აუქციონები და ანალიტიკა");
            texts["AboutFeatureAnalyticsBody"] = new LocalizedText("Работаем с Copart, Manheim и IAAI, подбираем релевантные лоты, проверяем автомобиль и оцениваем рентабельность сделки до участия в торгах.", "We work with Copart, Manheim, and IAAI, select relevant lots, inspect the vehicle, and estimate deal profitability before bidding.", "ვმუშაობთ Copart, Manheim და IAAI პლატფორმებთან, ვარჩევთ შესაბამის ლოტებს, ვამოწმებთ ავტომობილს და ვაფასებთ გარიგების ეფექტიანობას ტენდერში მონაწილეობამდე.");
            texts["AboutFeatureDocumentsTitle"] = new LocalizedText("Документы и сделка", "Documents and deal support", "დოკუმენტები და გარიგება");
            texts["AboutFeatureDocumentsBody"] = new LocalizedText("Сопровождаем перевод средств, страхование, оформление бумаг, подготовку к таможне, реэкспорту и выдаче автомобиля клиенту.", "We support fund transfers, insurance, paperwork, customs preparation, re-export, and final delivery to the client.", "თანხების გადარიცხვას, დაზღვევას, დოკუმენტების გაფორმებას, საბაჟოსთვის მომზადებას, რეექსპორტსა და ავტომობილის კლიენტისთვის გადაცემას ვუზრუნველყოფთ.");
            texts["AboutFeatureInvestmentTitle"] = new LocalizedText("Инвестиционный интерес", "Investment potential", "საინვესტიციო პოტენციალი");
            texts["AboutFeatureInvestmentBody"] = new LocalizedText("Компания работает не только с конечными клиентами, но и с инвесторами и партнерами, которым нужен понятный вход в автобизнес и проверенная операционная модель.", "The company works not only with end customers but also with investors and partners who need a clear entry into the car business and a proven operating model.", "კომპანია მუშაობს არა მხოლოდ საბოლოო კლიენტებთან, არამედ ინვესტორებთან და პარტნიორებთანაც, რომლებსაც სჭირდებათ ავტობიზნესში გასაგები შესვლა და გამოცდილი საოპერაციო მოდელი.");
            texts["CargoKicker"] = new LocalizedText("Что мы перевозим", "What we ship", "რას ვაზიდავთ");
            texts["CargoTitle"] = new LocalizedText("Категории транспорта, с которыми мы работаем", "Transport categories we work with", "ტრანსპორტის კატეგორიები, რომლებთანაც ვმუშაობთ");
            texts["CargoSedanTitle"] = new LocalizedText("Седан", "Sedan", "სედანი");
            texts["CargoSedanBody"] = new LocalizedText("Городские и семейные автомобили", "City and family vehicles", "ქალაქისა და ოჯახის ავტომობილები");
            texts["CargoSuvTitle"] = new LocalizedText("Внедорожник", "SUV", "ჯიპი");
            texts["CargoSuvBody"] = new LocalizedText("Кроссоверы и полноразмерные SUV", "Crossovers and full-size SUVs", "კროსოვერები და სრულზომიანი SUV");
            texts["CargoPickupTitle"] = new LocalizedText("Пикап", "Pickup", "პიკაპი");
            texts["CargoPickupBody"] = new LocalizedText("Рабочие и коммерческие решения", "Work and commercial solutions", "სამუშაო და კომერციული მოდელები");
            texts["CargoVanTitle"] = new LocalizedText("Микроавтобус", "Minibus", "მინივენი");
            texts["CargoVanBody"] = new LocalizedText("Пассажирские и грузопассажирские модели", "Passenger and mixed-use models", "სამგზავრო და სამგზავრო-სატვირთო მოდელები");
            texts["CargoMotorcycleTitle"] = new LocalizedText("Мотоцикл", "Motorcycle", "მოტოციკლი");
            texts["CargoMotorcycleBody"] = new LocalizedText("Двухколесная техника и кастом-проекты", "Two-wheel vehicles and custom projects", "ორბორბლიანი ტექნიკა და კასტომ-პროექტები");
            texts["CargoExclusiveTitle"] = new LocalizedText("Эксклюзивные авто", "Exclusive cars", "ექსკლუზიური ავტომობილები");
            texts["CargoExclusiveBody"] = new LocalizedText("Редкие и коллекционные автомобили", "Rare and collectible vehicles", "იშვიათი და საკოლექციო ავტომობილები");
            texts["CargoBoatsTitle"] = new LocalizedText("Катеры", "Boats", "ნავები");
            texts["CargoBoatsBody"] = new LocalizedText("Малогабаритная водная техника", "Small-size watercraft", "მცირე ზომის წყლის ტექნიკა");
            texts["CargoBuggyTitle"] = new LocalizedText("Багги", "Buggy", "ბაგი");
            texts["CargoBuggyBody"] = new LocalizedText("Спортивные и внедорожные модели", "Sport and off-road models", "სპორტული და უგზოობის მოდელები");
            texts["AdvantagesKicker"] = new LocalizedText("Наши преимущества", "Our advantages", "ჩვენი უპირატესობები");
            texts["AdvantagesTitle"] = new LocalizedText("Почему выбирают GLC Express", "Why clients choose GLC Express", "რატომ ირჩევენ GLC Express-ს");
            texts["AdvantageYearsTitle"] = new LocalizedText("10 лет успешной работы", "10 years of successful work", "10 წლიანი წარმატებული გამოცდილება");
            texts["AdvantageYearsBody"] = new LocalizedText("Практика на рынке США и понятные процессы для клиента.", "Practical USA market experience and clear processes for the client.", "აშშ-ის ბაზრის პრაქტიკული გამოცდილება და კლიენტისთვის გასაგები პროცესები.");
            texts["AdvantageTransparentTitle"] = new LocalizedText("Официальная и прозрачная работа", "Transparent and compliant operations", "ოფიციალური და გამჭვირვალე მუშაობა");
            texts["AdvantageTransparentBody"] = new LocalizedText("Фиксируем этапы сделки и делаем ценообразование понятным.", "We document each deal stage and keep pricing transparent.", "ვაფიქსირებთ გარიგების ეტაპებს და ფასების ჩამოყალიბებას გასაგებს ვხდით.");
            texts["AdvantageBiddingTitle"] = new LocalizedText("Доступ к торгам", "Auction access", "ვაჭრობაზე წვდომა");
            texts["AdvantageBiddingBody"] = new LocalizedText("Показываем динамику аукциона и помогаем управлять ставкой.", "We show auction dynamics and help manage the bid.", "ვაჩვენებთ აუქციონის დინამიკას და ვეხმარებით შეთავაზების მართვაში.");
            texts["AdvantageInspectionTitle"] = new LocalizedText("Бесплатная экспертиза ТС в USA", "Free vehicle inspection in the USA", "უფასო ავტომობილის ექსპერტიზა აშშ-ში");
            texts["AdvantageInspectionBody"] = new LocalizedText("Предварительная оценка помогает избежать слабых лотов.", "Initial assessment helps avoid weak lots.", "წინასწარი შეფასება სუსტი ლოტების თავიდან აცილებაში გვეხმარება.");
            texts["AdvantageLogisticsTitle"] = new LocalizedText("Низкая стоимость логистики", "Optimized logistics cost", "ოპტიმიზებული ლოჯისტიკის ღირებულება");
            texts["AdvantageLogisticsBody"] = new LocalizedText("Оптимизируем морской этап и контейнерную загрузку.", "We optimize sea shipping and container loading.", "ვაუმჯობესებთ საზღვაო გადაზიდვას და კონტეინერის დატვირთვას.");
            texts["AdvantageTrackingTitle"] = new LocalizedText("Online-отслеживание", "Online tracking", "ონლაინ თვალთვალი");
            texts["AdvantageTrackingBody"] = new LocalizedText("Клиент видит статус груза и ключевые точки маршрута.", "The client sees cargo status and key route milestones.", "კლიენტი ხედავს ტვირთის სტატუსს და მარშრუტის მთავარ ეტაპებს.");
            texts["AdvantageInsuranceTitle"] = new LocalizedText("Гарантии и страхование", "Guarantees and insurance", "გარანტიები და დაზღვევა");
            texts["AdvantageInsuranceBody"] = new LocalizedText("Делаем поставку более прогнозируемой и управляемой.", "We make delivery more predictable and controllable.", "ვხდით მიწოდებას უფრო პროგნოზირებად და მართვადს.");
            texts["AdvantageManagerTitle"] = new LocalizedText("Персональный менеджер", "Dedicated manager", "პერსონალური მენეჯერი");
            texts["AdvantageManagerBody"] = new LocalizedText("Один контакт на всех этапах проекта: от покупки до выдачи.", "One contact for every stage of the project: from purchase to final handover.", "ერთი კონტაქტი პროექტის ყველა ეტაპზე: შეძენიდან გადაცემამდე.");
            texts["AdvantageSupportTitle"] = new LocalizedText("Поддержка 24/7", "24/7 support", "24/7 მხარდაჭერა");
            texts["AdvantageSupportBody"] = new LocalizedText("Быстрые ответы по логистике, документам и торгам.", "Fast responses on logistics, documents, and auctions.", "სწრაფი პასუხები ლოჯისტიკაზე, დოკუმენტებსა და აუქციონებზე.");
            texts["TurnkeyKicker"] = new LocalizedText("Заказ автомобиля под ключ", "Turnkey car order", "ავტომობილის შეკვეთა სრული სერვისით");
            texts["TurnkeyTitle"] = new LocalizedText("Полный коммерческий цикл от запроса до выдачи автомобиля", "A full commercial cycle from request to vehicle handover", "სრული კომერციული ციკლი მოთხოვნიდან ავტომობილის გადაცემამდე");
            texts["TurnkeyLead"] = new LocalizedText("Это ключевой сервис GLC Express: клиент видит каждый этап, а команда закрывает операционные, логистические и документальные задачи под одной крышей.", "This is the core GLC Express service: the client sees every stage while one team handles operational, logistics, and paperwork tasks under one roof.", "ეს არის GLC Express-ის მთავარი სერვისი: კლიენტი ხედავს ყველა ეტაპს, ხოლო ერთი გუნდი ერთ სივრცეში ფარავს ოპერაციულ, ლოჯისტიკურ და დოკუმენტურ ამოცანებს.");
            texts["TurnkeyStep1Title"] = new LocalizedText("Подбор ТС", "Vehicle selection", "ავტომობილის შერჩევა");
            texts["TurnkeyStep1Body"] = new LocalizedText("Формируем shortlist по бюджету, задаче и рынку.", "We build a shortlist based on budget, task, and market.", "ვქმნით შორტლისტს ბიუჯეტის, ამოცანისა და ბაზრის მიხედვით.");
            texts["TurnkeyStep2Title"] = new LocalizedText("Проверка юридической чистоты", "Legal due diligence", "იურიდიული სისუფთავის შემოწმება");
            texts["TurnkeyStep2Body"] = new LocalizedText("Анализируем историю, документы и риски.", "We analyze history, documents, and risks.", "ვიკვლევთ ისტორიას, დოკუმენტებსა და რისკებს.");
            texts["TurnkeyStep3Title"] = new LocalizedText("Торги с участием клиента", "Bidding with client participation", "ვაჭრობა კლიენტის მონაწილეობით");
            texts["TurnkeyStep3Body"] = new LocalizedText("При необходимости клиент наблюдает за аукционом online.", "If needed, the client can watch the auction online.", "საჭიროების შემთხვევაში კლიენტი აუქციონს ონლაინ აკვირდება.");
            texts["TurnkeyStep4Title"] = new LocalizedText("Документы и страхование", "Documents and insurance", "დოკუმენტები და დაზღვევა");
            texts["TurnkeyStep4Body"] = new LocalizedText("Готовим комплект для перевозки и последующих операций.", "We prepare a full set of documents for shipping and later operations.", "ვამზადებთ დოკუმენტების სრულ პაკეტს გადაზიდვისა და შემდგომი ოპერაციებისთვის.");
            texts["TurnkeyStep5Title"] = new LocalizedText("Проведение транзакции", "Transaction support", "ტრანზაქციის შესრულება");
            texts["TurnkeyStep5Body"] = new LocalizedText("Сопровождаем оплату и подтверждение покупки.", "We support payment and purchase confirmation.", "ვუზრუნველყოფთ გადახდასა და შეძენის დადასტურებას.");
            texts["TurnkeyStep6Title"] = new LocalizedText("Отправка из USA", "Dispatch from the USA", "გაგზავნა აშშ-დან");
            texts["TurnkeyStep6Body"] = new LocalizedText("Организуем вывоз с площадки и загрузку в порт.", "We arrange pickup from the yard and loading at the port.", "ვაწყობთ გატანას პლატფორმიდან და დატვირთვას პორტში.");
            texts["TurnkeyStep7Title"] = new LocalizedText("Таможенный терминал", "Customs terminal", "საბაჟო ტერმინალი");
            texts["TurnkeyStep7Body"] = new LocalizedText("Принимаем авто и готовим его к дальнейшему маршруту.", "We receive the vehicle and prepare it for the next stage of the route.", "ვიღებთ ავტომობილს და ვამზადებთ მას მარშრუტის შემდეგი ეტაპისთვის.");
            texts["TurnkeyStep8Title"] = new LocalizedText("Восстановление повреждений", "Damage restoration", "დაზიანებების აღდგენა");
            texts["TurnkeyStep8Body"] = new LocalizedText("Выполняем ремонт и доводим автомобиль до нужного состояния.", "We repair the vehicle and bring it to the required condition.", "ვასრულებთ შეკეთებას და ავტომობილს საჭირო მდგომარეობამდე მივყავართ.");
            texts["TurnkeyStep9Title"] = new LocalizedText("Реэкспорт или растаможка", "Re-export or customs clearance", "რეექსპორტი ან განბაჟება");
            texts["TurnkeyStep9Body"] = new LocalizedText("Готовим пакет документов под целевой сценарий.", "We prepare a document package for the target scenario.", "ვამზადებთ დოკუმენტების პაკეტს მიზნობრივი სცენარისთვის.");
            texts["TurnkeyStep10Title"] = new LocalizedText("Передача клиенту", "Client handover", "კლიენტისთვის გადაცემა");
            texts["TurnkeyStep10Body"] = new LocalizedText("Финальный контроль и выдача готового автомобиля.", "Final quality control and handover of the ready vehicle.", "ფინალური კონტროლი და მზა ავტომობილის გადაცემა.");
            texts["AuctionKicker"] = new LocalizedText("Аукционный центр", "Auction center", "აუქციონის ცენტრი");
            texts["AuctionTitle"] = new LocalizedText("GLC Express как операционный центр по работе с лотами", "GLC Express as an operating center for lot management", "GLC Express როგორც ლოტებთან მუშაობის ოპერაციული ცენტრი");
            texts["AuctionLead"] = new LocalizedText("Аукционный блок на сайте выделен как отдельное направление, потому что именно здесь создается ценность сделки: команда помогает найти релевантный лот, сделать аналитику, оценить риски восстановления и выиграть торги в комфортной для клиента стратегии.", "The auction block is highlighted as a separate direction because this is where the deal value is created: the team helps find the right lot, perform analysis, estimate restoration risks, and win the auction within a client-friendly strategy.", "აუქციონის ბლოკი ცალკე მიმართულებად არის გამოყოფილი, რადგან სწორედ აქ იქმნება გარიგების ღირებულება: გუნდი ეხმარება შესაბამისი ლოტის მოძიებაში, ანალიტიკის გაკეთებაში, აღდგენის რისკების შეფასებასა და კლიენტისთვის კომფორტული სტრატეგიით გამარჯვებაში.");
            texts["AuctionCheck1"] = new LocalizedText("Подбор нужного транспортного средства под параметры заказчика", "Selection of the right vehicle for the client requirements", "სწორი სატრანსპორტო საშუალების შერჩევა კლიენტის მოთხოვნების მიხედვით");
            texts["AuctionCheck2"] = new LocalizedText("Аналитика и экспертиза состояния автомобиля до ставки", "Analysis and condition inspection before bidding", "ანალიტიკა და ავტომობილის მდგომარეობის ექსპერტიზა შეთავაზებამდე");
            texts["AuctionCheck3"] = new LocalizedText("Выигрыш лота с контролем бюджета и реальной ценности", "Winning the lot with budget control and real value assessment", "ლოტის მოგება ბიუჯეტის კონტროლით და რეალური ღირებულების შეფასებით");
            texts["AuctionCheck4"] = new LocalizedText("Страхование и оперативная отправка в порт назначения", "Insurance and prompt dispatch to the destination port", "დაზღვევა და ოპერატიული გაგზავნა დანიშნულების პორტში");
            texts["AuctionCheck5"] = new LocalizedText("Online-наблюдение за торгами и корректировка ставки в реальном времени", "Online auction watching and real-time bid adjustment", "აუქციონის ონლაინ მონიტორინგი და რეალურ დროში შეთავაზების კორექტირება");
            texts["AuctionInfoPlatformsLabel"] = new LocalizedText("Работаем с площадками", "Platforms we work with", "პლატფორმები, რომლებთანაც ვმუშაობთ");
            texts["AuctionInfoPlatformsValue"] = new LocalizedText("Copart, IAAI, Manheim", "Copart, IAAI, Manheim", "Copart, IAAI, Manheim");
            texts["AuctionInfoFocusLabel"] = new LocalizedText("Фокус", "Focus", "ფოკუსი");
            texts["AuctionInfoFocusValue"] = new LocalizedText("Понять стоимость восстановления до того, как вы купили лот", "Understand restoration cost before you buy the lot", "აღდგენის ღირებულების გაგება მანამდე, სანამ ლოტს შეიძენთ");
            texts["AuctionInfoResultLabel"] = new LocalizedText("Результат", "Result", "შედეგი");
            texts["AuctionInfoResultValue"] = new LocalizedText("Контролируемая покупка, а не ставка вслепую", "A controlled purchase, not a blind bid", "კონტროლირებადი შეძენა და არა ბრმა შეთავაზება");
            texts["DeliveryKicker"] = new LocalizedText("Доставка", "Delivery", "მიწოდება");
            texts["DeliveryTitle"] = new LocalizedText("Морская логистика из США с приемкой в Поти или Батуми", "Sea logistics from the USA with receiving in Poti or Batumi", "საზღვაო ლოჯისტიკა აშშ-დან მიღებით ფოთში ან ბათუმში");
            texts["DeliveryStep1Title"] = new LocalizedText("Этап 1. От площадки продажи до порта погрузки", "Stage 1. From the auction yard to the loading port", "ეტაპი 1. გაყიდვის მოედნიდან დატვირთვის პორტამდე");
            texts["DeliveryStep1Body"] = new LocalizedText("Организуем перемещение автомобиля от аукциона до порта, готовим фото- и видеофиксацию при загрузке и собираем пакет документов на отправку.", "We arrange transportation from the auction yard to the port, prepare photo and video evidence at loading, and collect the document package for shipment.", "ვაწყობთ ავტომობილის გადაადგილებას აუქციონიდან პორტამდე, ვამზადებთ ფოტო- და ვიდეოფიქსაციას დატვირთვისას და ვაგროვებთ დოკუმენტების პაკეტს გაგზავნისთვის.");
            texts["DeliveryStep2Title"] = new LocalizedText("Этап 2. Морская доставка до Грузии", "Stage 2. Sea shipment to Georgia", "ეტაპი 2. საზღვაო გადაზიდვა საქართველოში");
            texts["DeliveryStep2Body"] = new LocalizedText("Используем контейнерную схему до четырех автомобилей в одном контейнере. Средний срок доставки в Грузию — от 45 до 70 дней.", "We use a container model of up to four vehicles per container. The average delivery time to Georgia is 45 to 70 days.", "ვიყენებთ კონტეინერულ სქემას ერთ კონტეინერში ოთხამდე ავტომობილით. საქართველოში მიწოდების საშუალო ვადაა 45-70 დღე.");
            texts["DeliveryStep3Title"] = new LocalizedText("Этап 3. Растаможка или реэкспорт", "Stage 3. Customs clearance or re-export", "ეტაპი 3. განბაჟება ან რეექსპორტი");
            texts["DeliveryStep3Body"] = new LocalizedText("Готовим документы под рынок клиента и сопровождаем дальнейшее движение в Грузию, Россию, Казахстан, Армению, Киргизию и другие страны.", "We prepare documents for the client market and support onward movement to Georgia, Russia, Kazakhstan, Armenia, Kyrgyzstan, and other countries.", "ვამზადებთ დოკუმენტებს კლიენტის ბაზრისთვის და ვუზრუნველყოფთ შემდგომ გადაადგილებას საქართველოში, რუსეთში, ყაზახეთში, სომხეთში, ყირგიზეთში და სხვა ქვეყნებში.");
            texts["DeliveryStripPortsLabel"] = new LocalizedText("Порты прибытия", "Arrival ports", "ჩამოსვლის პორტები");
            texts["DeliveryStripPortsValue"] = new LocalizedText("Поти / Батуми", "Poti / Batumi", "ფოთი / ბათუმი");
            texts["DeliveryStripFormatLabel"] = new LocalizedText("Формат загрузки", "Loading format", "დატვირთვის ფორმატი");
            texts["DeliveryStripFormatValue"] = new LocalizedText("4 автомобиля в контейнере", "4 vehicles per container", "4 ავტომობილი კონტეინერში");
            texts["DeliveryStripControlLabel"] = new LocalizedText("Контроль", "Control", "კონტროლი");
            texts["DeliveryStripControlValue"] = new LocalizedText("Фото и видео на этапе погрузки", "Photos and videos during loading", "ფოტო და ვიდეო დატვირთვის ეტაპზე");
            texts["RepairKicker"] = new LocalizedText("Ремонт автомобилей", "Vehicle repair", "ავტომობილების შეკეთება");
            texts["RepairTitle"] = new LocalizedText("Восстановление после доставки и подготовка к эксплуатации", "Restoration after delivery and preparation for operation", "აღდგენა მიწოდების შემდეგ და ექსპლუატაციისთვის მომზადება");
            texts["RepairLead"] = new LocalizedText("После прибытия в Грузию GLC Express закрывает восстановительный контур: от сложных кузовных работ и электрики до русификации и обновления навигации. Это помогает клиенту получить не просто доставленный лот, а готовый автомобиль.", "After arrival in Georgia, GLC Express closes the restoration loop: from complex bodywork and electrical repairs to localization and navigation updates. This helps the client receive not just a delivered lot, but a ready vehicle.", "საქართველოში ჩამოსვლის შემდეგ GLC Express ფარავს აღდგენის სრულ ციკლს: რთული ძარისა და ელექტრო სამუშაოებიდან ლოკალიზაციამდე და ნავიგაციის განახლებამდე. შედეგად კლიენტი იღებს არა უბრალოდ ჩამოტანილ ლოტს, არამედ მზა ავტომობილს.");
            texts["RepairTag1"] = new LocalizedText("Кузовные элементы", "Body parts", "ძარის დეტალები");
            texts["RepairTag2"] = new LocalizedText("Лонжероны", "Frame rails", "ლონჟერონები");
            texts["RepairTag3"] = new LocalizedText("Подушки безопасности", "Airbags", "აირბაგები");
            texts["RepairTag4"] = new LocalizedText("Электрика", "Electrical systems", "ელექტროობა");
            texts["RepairTag5"] = new LocalizedText("Ходовая часть", "Suspension", "სავალი ნაწილი");
            texts["RepairTag6"] = new LocalizedText("Трансмиссия", "Transmission", "ტრანსმისია");
            texts["RepairTag7"] = new LocalizedText("Лакокрасочное покрытие", "Paintwork", "საღებავი საფარი");
            texts["RepairTag8"] = new LocalizedText("Русификация", "Localization", "ლოკალიზაცია");
            texts["RepairTag9"] = new LocalizedText("Навигация", "Navigation", "ნავიგაცია");
            texts["RepairTag10"] = new LocalizedText("Прошивка", "Firmware", "პროგრამული განახლება");
            texts["PartnerKicker"] = new LocalizedText("Партнерская программа", "Partner program", "პარტნიორული პროგრამა");
            texts["PartnerTitle"] = new LocalizedText("Станьте нашим партнером", "Become our partner", "გახდით ჩვენი პარტნიორი");
            texts["PartnerLead"] = new LocalizedText("Откройте свой автобизнес вместе с GLC Express уже сегодня: используйте нашу логистику, операционную экспертизу и доступ к аукционам как платформу для собственного роста.", "Launch your car business with GLC Express today: use our logistics, operational expertise, and auction access as a platform for your growth.", "დაიწყეთ თქვენი ავტობიზნესი GLC Express-თან ერთად უკვე დღეს: გამოიყენეთ ჩვენი ლოჯისტიკა, ოპერაციული ექსპერტიზა და აუქციონებზე წვდომა საკუთარი ზრდის პლატფორმად.");
            texts["PartnerCta"] = new LocalizedText("Обсудить партнерство", "Discuss partnership", "პარტნიორობის განხილვა");
            texts["PartnersKicker"] = new LocalizedText("Наши партнёры", "Our partners", "ჩვენი პარტნიორები");
            texts["PartnersTitle"] = new LocalizedText("Площадки и точки, которые формируют экосистему поставки", "Platforms and points that shape the delivery ecosystem", "პლატფორმები და წერტილები, რომლებიც მიწოდების ეკოსისტემას ქმნის");
            texts["ReviewsKicker"] = new LocalizedText("Отзывы", "Reviews", "შეფასებები");
            texts["ReviewsTitle"] = new LocalizedText("Почему клиенты возвращаются к нам снова", "Why clients come back to us", "რატომ ბრუნდებიან კლიენტები კვლავ ჩვენთან");
            texts["ContactsKicker"] = new LocalizedText("Контакты", "Contacts", "კონტაქტები");
            texts["ContactsTitle"] = new LocalizedText("Оставьте заявку и мы вернемся с маршрутом, сроками и расчетом", "Leave a request and we will return with a route, timing, and estimate", "დატოვეთ მოთხოვნა და ჩვენ დაგიბრუნდებით მარშრუტით, ვადებითა და ხარჯთაღრიცხვით");
            texts["ContactWhatsAppLabel"] = new LocalizedText("WhatsApp", "WhatsApp", "WhatsApp");
            texts["ContactAddressLabel"] = new LocalizedText("Адрес", "Address", "მისამართი");
            texts["ContactHoursLabel"] = new LocalizedText("Часы работы", "Working hours", "სამუშაო საათები");
            texts["ContactFormTitle"] = new LocalizedText("Запросить обратную связь", "Request a callback", "უკუკავშირის მოთხოვნა");
            texts["ContactFormLead"] = new LocalizedText("Оставьте контакты и параметры запроса, а при необходимости приложите файл с VIN, списком лотов или техническим заданием.", "Leave your contact details and request parameters, and attach a file with the VIN, list of lots, or technical brief if needed.", "დატოვეთ თქვენი საკონტაქტო მონაცემები და მოთხოვნის პარამეტრები, ხოლო საჭიროების შემთხვევაში დაამატეთ VIN-ით, ლოტების სიით ან ტექნიკური დავალებით ფაილი.");
            texts["FormNameLabel"] = new LocalizedText("Имя", "Name", "სახელი");
            texts["FormNameRequired"] = new LocalizedText("Укажите имя.", "Please enter your name.", "გთხოვთ მიუთითოთ სახელი.");
            texts["FormEmailLabel"] = new LocalizedText("Email", "Email", "Email");
            texts["FormEmailRequired"] = new LocalizedText("Укажите email.", "Please enter your email.", "გთხოვთ მიუთითოთ email.");
            texts["FormEmailInvalid"] = new LocalizedText("Укажите корректный email.", "Please enter a valid email address.", "გთხოვთ მიუთითოთ სწორი email.");
            texts["FormPhoneLabel"] = new LocalizedText("Телефон", "Phone", "ტელეფონი");
            texts["FormMessengerLabel"] = new LocalizedText("Мессенджер", "Messenger", "მესენჯერი");
            texts["FormMessengerPlaceholder"] = new LocalizedText("WhatsApp / Telegram / WeChat", "WhatsApp / Telegram / WeChat", "WhatsApp / Telegram / WeChat");
            texts["FormDirectionLabel"] = new LocalizedText("Направление", "Route", "მიმართულება");
            texts["FormDirectionPlaceholder"] = new LocalizedText("Например: США -> Грузия", "For example: USA -> Georgia", "მაგალითად: აშშ -> საქართველო");
            texts["FormCargoTypeLabel"] = new LocalizedText("Тип груза", "Cargo type", "ტვირთის ტიპი");
            texts["FormCargoUnselected"] = new LocalizedText("Не выбран", "Not selected", "არ არის არჩეული");
            texts["FormCargoSedan"] = new LocalizedText("Седан", "Sedan", "სედანი");
            texts["FormCargoSuv"] = new LocalizedText("Внедорожник", "SUV", "ჯიპი");
            texts["FormCargoPickup"] = new LocalizedText("Пикап", "Pickup", "პიკაპი");
            texts["FormCargoVan"] = new LocalizedText("Микроавтобус", "Minibus", "მინივენი");
            texts["FormCargoMotorcycle"] = new LocalizedText("Мотоцикл", "Motorcycle", "მოტოციკლი");
            texts["FormCargoExclusive"] = new LocalizedText("Эксклюзивные авто", "Exclusive cars", "ექსკლუზიური ავტომობილები");
            texts["FormCargoBoats"] = new LocalizedText("Катеры", "Boats", "ნავები");
            texts["FormCargoBuggy"] = new LocalizedText("Багги", "Buggy", "ბაგი");
            texts["FormCommentLabel"] = new LocalizedText("Комментарий", "Comment", "კომენტარი");
            texts["FormCommentPlaceholder"] = new LocalizedText("Опишите задачу, бюджет, VIN, интересующий лот или пожелания по доставке", "Describe the task, budget, VIN, target lot, or delivery requirements", "აღწერეთ ამოცანა, ბიუჯეტი, VIN, სასურველი ლოტი ან მიწოდების მოთხოვნები");
            texts["FormAttachmentLabel"] = new LocalizedText("Прикрепить файл", "Attach a file", "ფაილის დამატება");
            texts["FormAttachmentHint"] = new LocalizedText("Поддерживаются PDF и изображения. Файл необязателен.", "PDF files and images are supported. Attachment is optional.", "მხარდაჭერილია PDF ფაილები და გამოსახულებები. ფაილის დამატება სავალდებულო არ არის.");
            texts["FormSubmit"] = new LocalizedText("Отправить", "Send", "გაგზავნა");
            texts["HomeLeadAlertSuccess"] = new LocalizedText("Спасибо. Мы получили вашу заявку и свяжемся с вами в ближайшее время.", "Thank you. We have received your request and will contact you shortly.", "გმადლობთ. თქვენი მოთხოვნა მიღებულია და მალე დაგიკავშირდებით.");
            texts["InquiryErrorServerUnavailable"] = new LocalizedText("Сервер недоступен для сохранения заявки.", "The server is unavailable for saving your request.", "სერვერი მიუწვდომელია თქვენი მოთხოვნის შესანახად.");
            texts["InquiryErrorNameRequired"] = new LocalizedText("Укажите имя.", "Please enter your name.", "გთხოვთ მიუთითოთ სახელი.");
            texts["InquiryErrorAttachmentTooLarge"] = new LocalizedText("Размер вложения не должен превышать 5 МБ.", "The attachment size must not exceed 5 MB.", "დანართის ზომა არ უნდა აღემატებოდეს 5 მბ-ს.");
            texts["InquiryErrorAttachmentTypes"] = new LocalizedText("Для вложения разрешены только файлы: {0}", "Only these attachment file types are allowed: {0}", "დანართისთვის ნებადართულია მხოლოდ ეს ფაილის ტიპები: {0}");
            texts["InquiryErrorEmailInvalid"] = new LocalizedText("Укажите корректный email.", "Please enter a valid email address.", "გთხოვთ მიუთითოთ სწორი email.");
            texts["PrivacyPageTitle"] = new LocalizedText("GLC Express | Политика конфиденциальности", "GLC Express | Privacy Policy", "GLC Express | კონფიდენციალურობის პოლიტიკა");
            texts["PrivacyKicker"] = new LocalizedText("Privacy Policy", "Privacy Policy", "კონფიდენციალურობის პოლიტიკა");
            texts["PrivacyTitle"] = new LocalizedText("Политика конфиденциальности", "Privacy Policy", "კონფიდენციალურობის პოლიტიკა");
            texts["PrivacyMeta"] = new LocalizedText("Дата вступления в силу: 01.10.2022", "Effective date: 01.10.2022", "ძალაში შესვლის თარიღი: 01.10.2022");
            texts["PrivacyIntro"] = new LocalizedText("GLC Express уважает право каждого пользователя на конфиденциальность. Эта политика описывает, какие данные мы можем собирать, как используем информацию и каким образом пользователь может управлять своими данными при взаимодействии с сайтом компании.", "GLC Express respects every user's right to privacy. This policy describes what data we may collect, how we use the information, and how users can manage their data when interacting with the company website.", "GLC Express პატივს სცემს თითოეული მომხმარებლის კონფიდენციალურობის უფლებას. ეს პოლიტიკა აღწერს, რა მონაცემებს შეიძლება ვაგროვებდეთ, როგორ ვიყენებთ მათ და როგორ შეუძლია მომხმარებელს საკუთარი მონაცემების მართვა კომპანიის საიტთან ურთიერთობისას.");
            texts["PrivacyCollectedTitle"] = new LocalizedText("Какие данные мы собираем", "What data we collect", "რა მონაცემებს ვაგროვებთ");
            texts["PrivacyCollectedItem1"] = new LocalizedText("контактные данные, которые пользователь передает добровольно через формы обратной связи;", "contact information that the user voluntarily submits through feedback forms;", "საკონტაქტო მონაცემები, რომლებსაც მომხმარებელი ნებაყოფლობით აგზავნის უკუკავშირის ფორმებით;");
            texts["PrivacyCollectedItem2"] = new LocalizedText("технические данные браузера и устройства, включая cookie и информацию о посещении страниц;", "technical browser and device data, including cookies and page visit information;", "ბრაუზერისა და მოწყობილობის ტექნიკური მონაცემები, მათ შორის cookie ფაილები და გვერდების მონახულების ინფორმაცია;");
            texts["PrivacyCollectedItem3"] = new LocalizedText("файлы и документы, которые пользователь прикрепляет к заявке для расчета или проверки автомобиля.", "files and documents that the user attaches to a request for an estimate or vehicle inspection.", "ფაილები და დოკუმენტები, რომლებსაც მომხმარებელი ამატებს მოთხოვნას შეფასების ან ავტომობილის შემოწმებისთვის.");
            texts["PrivacyUsageTitle"] = new LocalizedText("Для чего используются данные", "How the data is used", "რისთვის გამოიყენება მონაცემები");
            texts["PrivacyUsageItem1"] = new LocalizedText("для обратной связи по заявкам, расчетам и подбору автомобилей;", "to respond to requests, estimates, and car sourcing inquiries;", "მოთხოვნებზე, შეფასებებსა და ავტომობილის შერჩევაზე უკუკავშირისთვის;");
            texts["PrivacyUsageItem2"] = new LocalizedText("для подготовки логистических и аукционных предложений;", "to prepare logistics and auction proposals;", "ლოჯისტიკური და აუქციონის შეთავაზებების მოსამზადებლად;");
            texts["PrivacyUsageItem3"] = new LocalizedText("для улучшения работы сайта, аналитики и повышения качества обслуживания;", "to improve website performance, analytics, and service quality;", "საიტის მუშაობის, ანალიტიკისა და მომსახურების ხარისხის გასაუმჯობესებლად;");
            texts["PrivacyUsageItem4"] = new LocalizedText("для выполнения юридических и договорных обязательств.", "to fulfill legal and contractual obligations.", "იურიდიული და სახელშეკრულებო ვალდებულებების შესასრულებლად.");
            texts["PrivacyThirdPartyTitle"] = new LocalizedText("Передача данных третьим лицам", "Data sharing with third parties", "მონაცემების გადაცემა მესამე პირებისთვის");
            texts["PrivacyThirdPartyBody"] = new LocalizedText("GLC Express не продает персональные данные. Информация может передаваться только тем контрагентам и сервисам, которые участвуют в оказании услуги: транспортным операторам, страховым компаниям, подрядчикам по документообороту и технической инфраструктуре сайта.", "GLC Express does not sell personal data. Information may be shared only with counterparties and services involved in delivering the service: transport operators, insurance companies, document processing vendors, and the website's technical infrastructure providers.", "GLC Express არ ყიდის პერსონალურ მონაცემებს. ინფორმაცია შეიძლება გადაეცეს მხოლოდ იმ კონტრაქტორებსა და სერვისებს, რომლებიც მომსახურების მიწოდებაში მონაწილეობენ: სატრანსპორტო ოპერატორებს, სადაზღვევო კომპანიებს, დოკუმენტბრუნვის კონტრაქტორებს და საიტის ტექნიკური ინფრასტრუქტურის მომწოდებლებს.");
            texts["PrivacyCookiesTitle"] = new LocalizedText("Cookie и аналитика", "Cookies and analytics", "Cookie და ანალიტიკა");
            texts["PrivacyCookiesBody"] = new LocalizedText("Сайт может использовать cookie для сохранения пользовательских предпочтений, аналитики и корректной работы сервисов. Пользователь может принять или отклонить cookie-уведомление, а также отключить cookie в настройках браузера.", "The website may use cookies to store user preferences, analytics, and proper service functionality. The user can accept or decline the cookie notice and can also disable cookies in the browser settings.", "საიტი შეიძლება იყენებდეს cookie ფაილებს მომხმარებლის არჩევანის დასამახსოვრებლად, ანალიტიკისთვის და სერვისების გამართული მუშაობისთვის. მომხმარებელს შეუძლია დათანხმდეს ან უარყოს cookie შეტყობინება, ასევე გამორთოს cookie ბრაუზერის პარამეტრებში.");
            texts["PrivacyRetentionTitle"] = new LocalizedText("Сроки хранения", "Retention period", "შენახვის ვადა");
            texts["PrivacyRetentionBody"] = new LocalizedText("Мы храним данные только в течение срока, необходимого для обработки заявки, сопровождения сделки, соблюдения требований законодательства или внутренней отчетности компании.", "We retain data only for as long as needed to process the request, support the deal, comply with legal requirements, or meet the company's internal reporting needs.", "მონაცემებს ვინახავთ მხოლოდ იმ ვადით, რაც საჭიროა მოთხოვნის დასამუშავებლად, გარიგების მხარდასაჭერად, კანონმდებლობის მოთხოვნების ან კომპანიის შიდა ანგარიშგების დასაცავად.");
            texts["PrivacyRightsTitle"] = new LocalizedText("Права пользователя", "User rights", "მომხმარებლის უფლებები");
            texts["PrivacyRightsItem1"] = new LocalizedText("запросить доступ к переданным данным;", "request access to the submitted data;", "მოთხოვოს წვდომა მიწოდებულ მონაცემებზე;");
            texts["PrivacyRightsItem2"] = new LocalizedText("потребовать исправления неточной информации;", "request correction of inaccurate information;", "მოითხოვოს არაზუსტი ინფორმაციის გასწორება;");
            texts["PrivacyRightsItem3"] = new LocalizedText("отозвать согласие на обработку данных, если это применимо;", "withdraw consent to data processing where applicable;", "გააუქმოს მონაცემთა დამუშავებაზე თანხმობა, თუ ეს შესაძლებელია;");
            texts["PrivacyRightsItem4"] = new LocalizedText("запросить удаление данных, если их хранение больше не требуется.", "request deletion of data if their storage is no longer required.", "მოითხოვოს მონაცემების წაშლა, თუ მათი შენახვა აღარ არის საჭირო.");
            texts["PrivacyContactTitle"] = new LocalizedText("Контактные данные", "Contact details", "საკონტაქტო ინფორმაცია");
            texts["PrivacyContactBody"] = new LocalizedText("По вопросам конфиденциальности и обработки данных можно обратиться в GLC Express через форму обратной связи на сайте, WhatsApp или по телефону +995 577 11 57 57.", "For privacy and data processing questions, you can contact GLC Express through the website contact form, WhatsApp, or by phone at +995 577 11 57 57.", "კონფიდენციალურობისა და მონაცემთა დამუშავების საკითხებზე შეგიძლიათ დაუკავშირდეთ GLC Express-ს საიტის საკონტაქტო ფორმით, WhatsApp-ით ან ტელეფონით +995 577 11 57 57.");

            return texts;
        }
    }
}
