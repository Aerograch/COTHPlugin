
:: this script needs https://www.nuget.org/packages/ilmerge

:: set your target executable name (typically [projectname].exe)
SET APP_NAME=COTHPlugin.dll

:: Set build, used for directory. Typically Release or Debug
SET ILMERGE_BUILD=Debug

:: Set platform, typically x64
SET ILMERGE_PLATFORM=x64

:: set your NuGet ILMerge Version, this is the number from the package manager install, for example:
:: PM> Install-Package ilmerge -Version 3.0.29
:: to confirm it is installed for a given project, see the packages.config file
SET ILMERGE_VERSION=3.0.29

:: the full ILMerge should be found here:
SET ILMERGE_PATH=%USERPROFILE%\.nuget\packages\ilmerge\%ILMERGE_VERSION%\tools\net452
:: dir "%ILMERGE_PATH%"\ILMerge.exe

echo Merging %APP_NAME% ...

:: add project DLL's starting with replacing the FirstLib with this project's DLL
"%ILMERGE_PATH%"\ILMerge.exe Bin\%ILMERGE_PLATFORM%\%ILMERGE_BUILD%\%APP_NAME%  ^
  /lib:Bin\%ILMERGE_PLATFORM%\%ILMERGE_BUILD%\ ^
  /out:%APP_NAME% ^
  Microsoft.Bcl.AsyncInterfaces.dll ^
  System.Buffers.dll ^
  System.Memory.dll ^
  System.Numerics.Vectors.dll ^
  System.Runtime.CompilerServices.Unsafe.dll ^
  System.Text.Encodings.Web.dll ^
  System.Text.Json.dll ^
  System.Threading.Tasks.Extensions.dll ^
  System.ValueTuple.dll


:Done
dir %APP_NAME%