%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil.exe bin\Debug\TLJ_PlayService.exe
Net Start TLJ_PlayService
sc config TLJ_PlayService start= auto

pause