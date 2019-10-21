# Jarvis
Jarvis was built as a way to manage static data files for automated database deployment with SqlPackage. Static data deployment is still in progress by Microsoft, and as yet is not supported.

## 
Static data files are built using merge scripts created with [generate-sql-merge](https://github.com/readyroll/generate-sql-merge/blob/master/master.dbo.sp_generate_merge.sql).

## Configuration
Create a normal database project using Visual Studio, and then add a `_Data` folder to the project to store static data files. Files can be organized in subfolders which represent different environments.

