@echo off
set logfile="C:\Users\Admin\Desktop\ReportGenerator\ReportGenerator\ReportGenerator\Scripts\events.log"

echo %date% %time% SOURCE=1C EVENTID=9001 MESSAGE=Information database list empty.>> %logfile%
