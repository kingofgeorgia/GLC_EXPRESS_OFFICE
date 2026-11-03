# Exercises Operations commands against in-memory CRM models; never opens the database.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Web
$root = Split-Path -Parent $PSScriptRoot
[void][Reflection.Assembly]::LoadFrom((Join-Path $root 'bin\GLC_EXPRESS.dll'))
$flags = [Reflection.BindingFlags]'Instance,NonPublic'
function Invoke-Operation($data, $action, $values) {
    $page = New-Object GLC_EXPRESS.Operations
    $request = New-Object System.Web.HttpRequest('', 'http://localhost/Operations', '')
    $form = $request.Form
    [Collections.Specialized.NameObjectCollectionBase].GetProperty('IsReadOnly', $flags).SetValue($form, $false, $null)
    foreach ($key in $values.Keys) { $form.Add($key, [string]$values[$key]) }
    $response = New-Object System.Web.HttpResponse((New-Object IO.StringWriter))
    $context = New-Object System.Web.HttpContext($request, $response)
    $context.User = New-Object Security.Principal.GenericPrincipal((New-Object Security.Principal.GenericIdentity('operations-test')), @('Admin'))
    [System.Web.UI.Page].GetField('_request', $flags).SetValue($page, $request)
    [System.Web.UI.Page].GetField('_context', $flags).SetValue($page, $context)
    $settings = New-Object GLC_EXPRESS.Models.CrmSettingsRecord
    $settings.DefaultTripStatus = 'New'
    $settings.TripStatuses = New-Object 'Collections.Generic.List[string]'
    $settings.TripStatuses.Add('New'); $settings.TripStatuses.Add('Done')
    $settings.ValidatePrepaymentAgainstFreight = $true
    $page.GetType().GetField('Settings', $flags).SetValue($page, $settings)
    try { $page.GetType().GetMethod('ApplyCommand', $flags).Invoke($page, @($data.PSObject.BaseObject, [string]$action)) }
    catch { throw $_.Exception.GetBaseException() }
}
function Assert($condition, $message) { if (!$condition) { throw $message } }
function Reject($data, $action, $values) {
    $rejected = $false
    try { Invoke-Operation $data $action $values } catch [ArgumentException] { $rejected = $true }
    Assert $rejected ('Expected rejection: ' + $action)
}
$data = New-Object GLC_EXPRESS.Models.CrmDataStore
$client = New-Object GLC_EXPRESS.Models.ClientRecord
$driver = New-Object GLC_EXPRESS.Models.DriverRecord
$vehicle = New-Object GLC_EXPRESS.Models.FleetVehicleRecord
$data.Clients.Add($client); $data.Drivers.Add($driver); $data.FleetVehicles.Add($vehicle)
$values = @{number='TEST-01'; country='Poti'; clientId=$client.Id; driverId=$driver.Id; vehicleId=$vehicle.Id; start='2026-10-23'; end='2026-10-24'; freight='1000'; advance='200'}
Invoke-Operation $data 'createTrip' $values
Assert ($data.Trips.Count -eq 1) 'Trip not created'
Reject $data 'createTrip' $values
$values.number='TEST-02'; $values.advance='2000'
Reject $data 'createTrip' $values
$values.advance='0'; $values.end='2026-10-22'
Reject $data 'createTrip' $values
Assert ($data.Trips.Count -eq 1) 'Rejected commands changed trips'
$car = New-Object GLC_EXPRESS.Models.CarRecord
$data.Cars.Add($car)
$trip = $data.Trips[0]
Invoke-Operation $data 'assignCar' @{tripId=$trip.Id; carId=$car.Id}
Assert ($car.TripNumber -eq 'TEST-01' -and $car.ChangeHistory.Count -eq 1) 'Assignment or history missing'
Reject $data 'assignCar' @{tripId=$trip.Id; carId=$car.Id}
Invoke-Operation $data 'resources' @{tripId=$trip.Id; resourceDriver=$driver.Id; resourceVehicle=$vehicle.Id}
Assert ($trip.DriverId -eq $driver.Id -and $trip.VehicleId -eq $vehicle.Id) 'Resources not saved'
Invoke-Operation $data 'status' @{tripId=$trip.Id; status='Done'}
Assert ($trip.Status -eq 'Done') 'Status not saved'
Reject $data 'status' @{tripId=$trip.Id; status='invalid'}
Reject $data 'resources' @{tripId=$trip.Id; resourceDriver='missing'; resourceVehicle=$vehicle.Id}
Reject $data 'status' @{tripId='missing'; status='Done'}
$duplicate = New-Object GLC_EXPRESS.Models.TripRecord
$duplicate.Number = $trip.Number
$data.Trips.Add($duplicate)
$car.TripNumber = ''
Reject $data 'assignCar' @{tripId=$trip.Id; carId=$car.Id}
$values.number='TEST-OPEN'; $values.end=''; $values.advance='0'
Invoke-Operation $data 'createTrip' $values
Assert ($data.Trips[$data.Trips.Count-1].EndDate -eq '') 'Optional end date was not preserved'
$values.number='TEST-BAD'; $values.end='invalid'
Reject $data 'createTrip' $values
$values.end=''; $values.freight='-1'
Reject $data 'createTrip' $values
Write-Output 'PASS: creation, duplicate numbers, amounts, dates, assignment/history, resources, status, stale references and ambiguous trip guards. No database writes.'

