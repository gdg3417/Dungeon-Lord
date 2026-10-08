param([int[]]$TargetProcessIds = @(), [string]$ReportPath)
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class RoomClipboardContext {
 [DllImport("user32.dll", SetLastError=true)] public static extern IntPtr GetProcessWindowStation();
 [DllImport("user32.dll", SetLastError=true)] public static extern IntPtr GetThreadDesktop(uint id);
 [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
 [DllImport("user32.dll", CharSet=CharSet.Unicode, SetLastError=true)] public static extern bool GetUserObjectInformation(IntPtr handle, int index, StringBuilder value, uint length, out uint needed);
 [DllImport("user32.dll", SetLastError=true)] public static extern IntPtr OpenInputDesktop(uint flags, bool inherit, uint access);
 [DllImport("user32.dll")] public static extern bool CloseDesktop(IntPtr handle);
 [DllImport("user32.dll", SetLastError=true)] public static extern bool OpenClipboard(IntPtr owner);
 [DllImport("user32.dll")] public static extern bool CloseClipboard();
 [DllImport("user32.dll")] public static extern IntPtr GetOpenClipboardWindow();
 [DllImport("kernel32.dll", SetLastError=true)] public static extern IntPtr OpenProcess(uint access, bool inherit, int id);
 [DllImport("advapi32.dll", SetLastError=true)] public static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);
 [DllImport("advapi32.dll", SetLastError=true)] public static extern bool OpenThreadToken(IntPtr thread, uint access, bool asSelf, out IntPtr token);
 [DllImport("advapi32.dll", SetLastError=true)] public static extern bool GetTokenInformation(IntPtr token, int kind, IntPtr buffer, uint length, out uint needed);
 [DllImport("advapi32.dll")] public static extern bool IsTokenRestricted(IntPtr token);
 [DllImport("kernel32.dll")] public static extern bool CloseHandle(IntPtr handle);
 [DllImport("kernel32.dll", SetLastError=true)] public static extern bool QueryInformationJobObject(IntPtr job, int kind, out uint value, uint length, out uint returned);
 [DllImport("kernel32.dll", SetLastError=true)] public static extern bool IsProcessInJob(IntPtr process, IntPtr job, out bool inside);
 [DllImport("user32.dll", CharSet=CharSet.Unicode, SetLastError=true)] public static extern IntPtr OpenWindowStation(string name, bool inherit, uint access);
 [DllImport("user32.dll")] public static extern bool CloseWindowStation(IntPtr handle);
 [DllImport("ntdll.dll")] public static extern int NtQueryObject(IntPtr handle, int kind, byte[] buffer, uint length, out uint returned);
}
'@
function ObjectName([IntPtr]$handle) {
 $value = [Text.StringBuilder]::new(512); [uint32]$needed=0
 $ok=[RoomClipboardContext]::GetUserObjectInformation($handle,2,$value,1024,[ref]$needed)
 $errorCode=[Runtime.InteropServices.Marshal]::GetLastWin32Error()
 if($ok){return $value.ToString()}; return ('Unavailable (error '+$errorCode+')')
}
function TokenValue([IntPtr]$token,[int]$kind) {
 [uint32]$size=0; [void][RoomClipboardContext]::GetTokenInformation($token,$kind,[IntPtr]::Zero,0,[ref]$size)
 if($size -eq 0){return $null}
 $buffer=[Runtime.InteropServices.Marshal]::AllocHGlobal([int]$size)
 try {
  if(-not [RoomClipboardContext]::GetTokenInformation($token,$kind,$buffer,$size,[ref]$size)){return $null}
  if($kind -eq 25){return [Security.Principal.SecurityIdentifier]::new([Runtime.InteropServices.Marshal]::ReadIntPtr($buffer)).Value}
  return [Runtime.InteropServices.Marshal]::ReadInt32($buffer)
 } finally { [Runtime.InteropServices.Marshal]::FreeHGlobal($buffer) }
}
function ProcessContext([int]$processId) {
 $process=[RoomClipboardContext]::OpenProcess(4096,$false,$processId)
 if($process -eq [IntPtr]::Zero){return [pscustomobject]@{ProcessId=$processId;QueryError=[Runtime.InteropServices.Marshal]::GetLastWin32Error()}}
 $token=[IntPtr]::Zero
 try {
  if(-not [RoomClipboardContext]::OpenProcessToken($process,8,[ref]$token)){return [pscustomobject]@{ProcessId=$processId;TokenQueryError=[Runtime.InteropServices.Marshal]::GetLastWin32Error()}}
  [pscustomobject]@{ProcessId=$processId;Session=TokenValue $token 12;IntegritySID=TokenValue $token 25;IsAppContainer=TokenValue $token 29;ElevationType=TokenValue $token 18;RestrictedSIDCount=TokenValue $token 11;IsRestricted=[RoomClipboardContext]::IsTokenRestricted($token)}
 } finally {if($token -ne [IntPtr]::Zero){[void][RoomClipboardContext]::CloseHandle($token)};[void][RoomClipboardContext]::CloseHandle($process)}
}
$station=ObjectName ([RoomClipboardContext]::GetProcessWindowStation())
$desktop=ObjectName ([RoomClipboardContext]::GetThreadDesktop([RoomClipboardContext]::GetCurrentThreadId()))
$inputDesktopHandle=[RoomClipboardContext]::OpenInputDesktop(0,$false,1)
$inputError=[Runtime.InteropServices.Marshal]::GetLastWin32Error()
$inputName=if($inputDesktopHandle -ne [IntPtr]::Zero){ObjectName $inputDesktopHandle}else{'Unavailable'}
if($inputDesktopHandle -ne [IntPtr]::Zero){[void][RoomClipboardContext]::CloseDesktop($inputDesktopHandle)}
$held=[RoomClipboardContext]::GetOpenClipboardWindow() -ne [IntPtr]::Zero
$opened=[RoomClipboardContext]::OpenClipboard([IntPtr]::Zero)
$clipboardError=[Runtime.InteropServices.Marshal]::GetLastWin32Error()
if($opened){[void][RoomClipboardContext]::CloseClipboard()}
[uint32]$jobUI=0; [uint32]$returned=0
$jobQuery=[RoomClipboardContext]::QueryInformationJobObject([IntPtr]::Zero,4,[ref]$jobUI,4,[ref]$returned)
$jobError=[Runtime.InteropServices.Marshal]::GetLastWin32Error()
$stationHandle=[RoomClipboardContext]::GetProcessWindowStation()
$stationBuffer=[byte[]]::new(56); [uint32]$stationReturned=0
$stationQuery=[RoomClipboardContext]::NtQueryObject($stationHandle,0,$stationBuffer,56,[ref]$stationReturned)
$stationGranted=if($stationQuery -eq 0){[BitConverter]::ToUInt32($stationBuffer,4)}else{$null}
$threadToken=[IntPtr]::Zero
$threadHasToken=[RoomClipboardContext]::OpenThreadToken([IntPtr]::new(-2),8,$true,[ref]$threadToken)
$threadError=[Runtime.InteropServices.Marshal]::GetLastWin32Error()
$threadContext=if($threadHasToken){[pscustomobject]@{IntegritySID=TokenValue $threadToken 25;IsAppContainer=TokenValue $threadToken 29;IsRestricted=[RoomClipboardContext]::IsTokenRestricted($threadToken)}}else{$null}
if($threadHasToken){[void][RoomClipboardContext]::CloseHandle($threadToken)}
$stationClipboard=[RoomClipboardContext]::OpenWindowStation($station,$false,4)
$stationError=[Runtime.InteropServices.Marshal]::GetLastWin32Error()
if($stationClipboard -ne [IntPtr]::Zero){[void][RoomClipboardContext]::CloseWindowStation($stationClipboard)}
$report=[pscustomobject]@{UserInteractive=[Environment]::UserInteractive;WindowStation=$station;WindowStationQueryStatus=$stationQuery;WindowStationGrantedAccess=$stationGranted;AssignedStationHasClipboardAccess=$(if($stationQuery -eq 0){($stationGranted -band 4) -ne 0}else{$null});ThreadDesktop=$desktop;InputDesktop=$inputName;InputDesktopError=$(if($inputDesktopHandle -ne [IntPtr]::Zero){0}else{$inputError});ClipboardAlreadyOpen=$held;CanOpenClipboard=$opened;ClipboardError=$(if($opened){0}else{$clipboardError});ClipboardContentsRead=$false;ClipboardContentsChanged=$false;CanOpenStationForClipboard=($stationClipboard -ne [IntPtr]::Zero);StationClipboardAccessError=$(if($stationClipboard -ne [IntPtr]::Zero){0}else{$stationError});JobUIQuerySucceeded=$jobQuery;JobUIQueryError=$(if($jobQuery){0}else{$jobError});JobUIRestrictions=$jobUI;JobDisallowsClipboardRead=($jobQuery -and ($jobUI -band 2) -ne 0);JobDisallowsClipboardWrite=($jobQuery -and ($jobUI -band 4) -ne 0);Processes=@(ProcessContext $PID;foreach($targetId in $TargetProcessIds){ProcessContext $targetId})}
$report | Add-Member -NotePropertyName ThreadTokenQueryError -NotePropertyValue $(if($threadHasToken){0}else{$threadError})
$report | Add-Member -NotePropertyName ThreadImpersonationToken -NotePropertyValue $threadContext
$json=$report | ConvertTo-Json -Depth 5
if($ReportPath){[IO.File]::WriteAllText($ReportPath,$json)}
$json
