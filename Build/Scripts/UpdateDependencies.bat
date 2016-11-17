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

SET pwd="%CD%"
SET gwd="%LOCALAPPDATA%\Temp\openECA"
SET git="%PROGRAMFILES(X86)%\Git\cmd\git.exe"
SET replace="\\GPAWEB\NightlyBuilds\Tools\ReplaceInFiles\ReplaceInFiles.exe"

SET remote="git@github.com:GridProtectionAlliance/openECA.git"
SET source="\\GPAWEB\NightlyBuilds\GridSolutionsFramework\Beta\Libraries\*.*"
SET target="Source\Dependencies\GSF"
SET sourcemasterbuild="\\GPAWEB\NightlyBuilds\GridSolutionsFramework\Beta\Build Scripts\MasterBuild.buildproj"
SET targetmasterbuild="Build\Scripts"
SET sourceschema=Source\Dependencies\GSF\Data
SET targetschema=Source\Data
SET sourcetools=\\GPAWEB\NightlyBuilds\GridSolutionsFramework\Beta\Tools\
SET targettools=Source\Applications\openECA\openECASetup\

ECHO.
ECHO Entering working directory...
IF EXIST %gwd% IF NOT EXIST %gwd%\.git RMDIR /S /Q %gwd%
IF NOT EXIST %gwd% MKDIR %gwd%
CD /D %gwd%

IF EXIST .git GOTO UpdateRepository

ECHO.
ECHO Cloning remote repository...
%git% clone %remote% .

:UpdateRepository
ECHO.
ECHO Updating to latest version...
%git% fetch
%git% reset --hard origin/master
%git% clean -f -d -x

ECHO.
ECHO Updating dependencies...
XCOPY %source% %target% /Y /E
XCOPY %sourcemasterbuild% %targetmasterbuild% /Y
XCOPY "%sourcetools%ConfigCrypter\ConfigCrypter.exe" "%targettools%ConfigCrypter.exe" /Y
XCOPY "%sourcetools%ConfigEditor\ConfigEditor.exe" "%targettools%ConfigurationEditor.exe" /Y
XCOPY "%sourcetools%DataMigrationUtility\DataMigrationUtility.exe" "%targettools%DataMigrationUtility.exe" /Y
XCOPY "%sourcetools%HistorianPlaybackUtility\HistorianPlaybackUtility.exe" "%targettools%HistorianPlaybackUtility.exe" /Y
XCOPY "%sourcetools%HistorianView\HistorianView.exe" "%targettools%HistorianView.exe" /Y
XCOPY "%sourcetools%StatHistorianReportGenerator\StatHistorianReportGenerator.exe" "%targettools%StatHistorianReportGenerator.exe" /Y
XCOPY "%sourcetools%NoInetFixUtil\NoInetFixUtil.exe" "%targettools%NoInetFixUtil.exe" /Y
XCOPY "%sourcetools%DNP3ConfigGenerator\DNP3ConfigGenerator.exe" "%targettools%DNP3ConfigGenerator.exe" /Y
XCOPY "%sourcetools%LogFileViewer\LogFileViewer.exe" "%targettools%LogFileViewer.exe" /Y

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
%replace% /r /v "%targetschema%\*.sql" GSFSchema openECA
%replace% /r /v "%targetschema%\*.sql" "--*" "-- "
%replace% /r /v "%targetschema%\*SampleDataSet.sql" 8500 8525
%replace% /r /v "%targetschema%\*SampleDataSet.sql" 6165 6190
%replace% /r /v "%targetschema%\*SampleDataSet.sql" "e7a5235d-cb6f-4864-a96e-a8686f36e599" "e57b4a6d-ca9e-403c-ad2e-9a1db9a8a707"
%replace% /r /v "%targetschema%\*db-update.bat" GSFSchema openECA
CD %targetschema%\SQLite
CALL db-update.bat
CD %gwd%

ECHO.
ECHO Committing updates to local repository...
%git% add .
%git% commit -m "Updated GSF dependencies."

ECHO.
ECHO Pushing changes to remote repository...
%git% push
CD /D %pwd%

ECHO.
ECHO Update complete