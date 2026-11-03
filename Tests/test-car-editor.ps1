# Tests use only controls and in-memory models; no database writes.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Web
[void][Reflection.Assembly]::LoadFrom((Join-Path (Split-Path -Parent $PSScriptRoot) 'bin\GLC_EXPRESS.dll'))
$flags=[Reflection.BindingFlags]'Instance,NonPublic'
$page=New-Object GLC_EXPRESS.orders
$type=$page.GetType()
foreach($field in $type.GetFields($flags)) {
 if($field.FieldType -eq [System.Web.UI.WebControls.TextBox] -or $field.FieldType -eq [System.Web.UI.WebControls.DropDownList] -or $field.FieldType -eq [System.Web.UI.WebControls.Panel] -or $field.FieldType -eq [System.Web.UI.WebControls.Literal]) {
  $field.SetValue($page,[Activator]::CreateInstance($field.FieldType))
 }
}
function Control($name){$type.GetField($name,$flags).GetValue($page)}
function Assert($condition,$message){if(!$condition){throw $message}}
function Valid {$type.GetMethod('ValidateCarEditor',$flags).Invoke($page,@())}
(Control 'CarsDetailVinTextBox').Text='TESTVIN'
(Control 'CarsDetailLocationTextBox').Text='Poti'
Assert (Valid) 'Valid minimal car rejected'
(Control 'CarsDetailVinTextBox').Text=''
Assert (!(Valid)) 'Empty VIN accepted'
(Control 'CarsDetailVinTextBox').Text='TESTVIN'
(Control 'CarsDetailLocationTextBox').Text=''
Assert (!(Valid)) 'Empty location accepted'
(Control 'CarsDetailLocationTextBox').Text='Poti'
foreach($name in @('StartPrice','Volume','Power','PortCost','LoadingCost','TowTruckCost','ParkingCost','InspectionCost','ReExportCost','ExpertiseCost','DeliveryCost')) {
 $control=Control ('CarsDetail'+$name+'TextBox')
 foreach($bad in @('-1','abc')) {$control.Text=$bad; Assert (!(Valid)) ('Bad number accepted: '+$name)}
 $control.Text='12'; Assert (Valid) ('Valid number rejected: '+$name);$control.Text=''
}
(Control 'CarsDetailPowerTextBox').Text='1.5';Assert (!(Valid)) 'Fractional power accepted'
(Control 'CarsDetailPowerTextBox').Text=''
$forwarder=Control 'CarsDetailForwarderDropDownList'
$forwarder.Items.Add([System.Web.UI.WebControls.ListItem]::new('New','__new__'));$forwarder.SelectedValue='__new__'
Assert (!(Valid)) 'Empty new forwarder accepted'
(Control 'CarsNewForwarderTextBox').Text='Example Logistics';Assert (Valid) 'Named forwarder rejected'
$validateTrip=$type.GetMethod('TryValidateTripValues',$flags)
$argsTrip=@('2026-10-23','','100','0',[DateTime]::MinValue,[DateTime]::MinValue)
Assert ($validateTrip.Invoke($page,$argsTrip)) 'Optional CRM end date rejected'
Assert ($argsTrip[5] -eq [DateTime]::MinValue) 'Blank CRM end date changed'
$argsTrip=@('2026-10-23','2026-10-22','100','0',[DateTime]::MinValue,[DateTime]::MinValue)
Assert (!$validateTrip.Invoke($page,$argsTrip)) 'Earlier end date accepted'
Write-Output 'PASS: required VIN/location, numeric fields, integer power, new forwarder, optional CRM trip date and date order. No database writes.'
