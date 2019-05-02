::*******************************************************************************************************
::  UpdateDependencies.bat - Gbtc
::
::  Copyright � 2013, Grid Protection Alliance.  All Rights Reserved.
::
::  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
::  the NOTICE file distributed with this work for additional information regarding copyright ownership.
::  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
::  not use this file except in compliance with the License. You may obtain a copy of the License at:
::
::      http://www.opensource.org/licenses/eclipse-1.0.php
::
::  Unless agreed to in writing, the subject software distributed under the License is distributed on an
::  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
::  License for the specific language governing permissions and limitations.
::
::  Code Modification History:
::  -----------------------------------------------------------------------------------------------------
::  02/26/2011 - Pinal C. Patel
::       Generated original version of source code.
::  08/26/2013 - J. Ritchie Carroll
::       Updated to roll-down schema files from Grid Solutions Framework.
::
::*******************************************************************************************************

@ECHO OFF

SETLOCAL

SET pwd=%CD%
IF "%git%" == "" SET git=%PROGRAMFILES(X86)%\Git\cmd\git.exe
IF "%replace%" == "" SET replace=\\GPAWEB\NightlyBuilds\Tools\ReplaceInFiles\ReplaceInFiles.exe

SET defaulttarget=%LOCALAPPDATA%\Temp\openECA
IF "%remote%" == "" SET remote=git@github.com:GridProtectionAlliance/openECA.git
IF "%source%" == "" SET source=\\GPAWEB\NightlyBuilds\GridSolutionsFramework\Beta
IF "%sttp%" == "" SET sttp=\\GPAWEB\NightlyBuilds\sttp-gsfapi\Beta
IF "%target%" == "" SET target=%defaulttarget%

SET libraries=%source%\Libraries\*.*
SET sttplibrary=%sttp%\lib\sttp.gsf.dll
SET dependencies=%target%\Source\Dependencies\GSF
SET sourcemasterbuild=%source%\Build Scripts\MasterBuild.buildproj
SET targetmasterbuild=%target%\Build\Scripts
SET sourceschema=%target%\Source\Dependencies\GSF\Data
SET targetschema=%target%\Source\Data
SET sourcetools=%source%\Tools
SET targettools=%target%\Source\Applications\openECA\openECASetup

ECHO.
ECHO Entering working directory...
IF EXIST "%target%" IF NOT EXIST "%target%"\.git RMDIR /S /Q "%target%"
IF NOT EXIST "%target%" MKDIR "%target%"
CD /D %target%

IF NOT EXIST .git GOTO CloneRepository
IF NOT "%target%" == "%defaulttarget%" GOTO UpdateDependencies
GOTO UpdateRepository

:CloneRepository
ECHO.
ECHO Cloning remote repository...
"%git%" clone "%remote%" .
GOTO UpdateDependencies

:UpdateRepository
ECHO.
ECHO Updating to latest version...
"%git%" gc
"%git%" fetch
"%git%" reset --hard origin/master
"%git%" clean -f -d -x
GOTO UpdateDependencies

:UpdateDependencies
ECHO.
ECHO Updating dependencies...
XCOPY "%libraries%" "%dependencies%\" /Y /E
XCOPY "%sttplibrary%" "%dependencies%\" /Y
XCOPY "%sourcemasterbuild%" "%targetmasterbuild%\" /Y
COPY /Y "%sourcetools%\ConfigCrypter\ConfigCrypter.exe" "%targettools%\ConfigCrypter.exe"
COPY /Y "%sourcetools%\ConfigEditor\ConfigEditor.exe" "%targettools%\ConfigurationEditor.exe"
COPY /Y "%sourcetools%\CSVDataManager\CSVDataManager.exe" "%targettools%\CSVDataManager.exe"
COPY /Y "%sourcetools%\DataMigrationUtility\DataMigrationUtility.exe" "%targettools%\DataMigrationUtility.exe"
COPY /Y "%sourcetools%\HistorianPlaybackUtility\HistorianPlaybackUtility.exe" "%targettools%\HistorianPlaybackUtility.exe"
COPY /Y "%sourcetools%\HistorianView\HistorianView.exe" "%targettools%\HistorianView.exe"
COPY /Y "%sourcetools%\StatHistorianReportGenerator\StatHistorianReportGenerator.exe" "%targettools%\StatHistorianReportGenerator.exe"
COPY /Y "%sourcetools%\NoInetFixUtil\NoInetFixUtil.exe" "%targettools%\NoInetFixUtil.exe"
COPY /Y "%sourcetools%\DNP3ConfigGenerator\DNP3ConfigGenerator.exe" "%targettools%\DNP3ConfigGenerator.exe"
COPY /Y "%sourcetools%\LogFileViewer\LogFileViewer.exe" "%targettools%\LogFileViewer.exe"

:UpdateDbScripts
ECHO.
ECHO Updating database schema defintions...
FOR /R "%sourceschema%" %%x IN (*.db) DO DEL "%%x"
FOR /R "%sourceschema%" %%x IN (GSFSchema.*) DO REN "%%x" "openECA.*"
FOR /R "%sourceschema%" %%x IN (GSFSchema-InitialDataSet.*) DO REN "%%x" "openECA-InitialDataSet.*"
FOR /R "%sourceschema%" %%x IN (GSFSchema-SampleDataSet.*) DO REN "%%x" "openECA-SampleDataSet.*"
MOVE /Y "%sourceschema%\*.*" "%targetschema%\"
MOVE /Y "%sourceschema%\MySQL\*.*" "%targetschema%\MySQL\"
MOVE /Y "%sourceschema%\Oracle\*.*" "%targetschema%\Oracle\"
MOVE /Y "%sourceschema%\PostgreSQL\*.*" "%targetschema%\PostgreSQL\"
MOVE /Y "%sourceschema%\SQL Server\*.*" "%targetschema%\SQL Server\"
MOVE /Y "%sourceschema%\SQLite\*.*" "%targetschema%\SQLite\"
ECHO. >> "%targetschema%\MySQL\InitialDataSet.sql"
ECHO. >> "%targetschema%\Oracle\InitialDataSet.sql"
ECHO. >> "%targetschema%\PostgreSQL\InitialDataSet.sql"
ECHO. >> "%targetschema%\SQL Server\InitialDataSet.sql"
ECHO. >> "%targetschema%\SQLite\InitialDataSet.sql"
TYPE "%targetschema%\MySQL\_InitialDataSet.sql" >> "%targetschema%\MySQL\InitialDataSet.sql"
TYPE "%targetschema%\Oracle\_InitialDataSet.sql" >> "%targetschema%\Oracle\InitialDataSet.sql"
TYPE "%targetschema%\PostgreSQL\_InitialDataSet.sql" >> "%targetschema%\PostgreSQL\InitialDataSet.sql"
TYPE "%targetschema%\SQL Server\_InitialDataSet.sql" >> "%targetschema%\SQL Server\InitialDataSet.sql"
TYPE "%targetschema%\SQLite\_InitialDataSet.sql" >> "%targetschema%\SQLite\InitialDataSet.sql"
"%replace%" /r /v "%targetschema%\*.sql" GSFSchema openECA
"%replace%" /r /v "%targetschema%\*.sql" "--*" "-- "
"%replace%" /r /v "%targetschema%\*SampleDataSet.sql" 8500 8525
"%replace%" /r /v "%targetschema%\*SampleDataSet.sql" 6165 6190
"%replace%" /r /v "%targetschema%\*SampleDataSet.sql" "e7a5235d-cb6f-4864-a96e-a8686f36e599" "e57b4a6d-ca9e-403c-ad2e-9a1db9a8a707"
"%replace%" /r /v "%targetschema%\*db-update.bat" GSFSchema openECA
CD %targetschema%\SQLite
CALL db-update.bat
CD %target%

:CommitChanges
ECHO.
ECHO Committing updates to local repository...
"%git%" add .
"%git%" commit -m "Updated GSF dependencies."
IF NOT "%donotpush%" == "" GOTO Finish

:PushChanges
ECHO.
ECHO Pushing changes to remote repository...
"%git%" push
CD /D %pwd%

:Finish
ECHO.
ECHO Update complete