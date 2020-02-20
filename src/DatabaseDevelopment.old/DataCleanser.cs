using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace DatabaseDevelopment
{
    public class DataCleanser
    {
        public void ReplaceImagePathFields(DataTable dataTable, string serverName, string driveShare, string localPath)
        {
            List<string> errorMessages = new List<string>();
            if (dataTable.Columns.Contains("ImagePath"))
            {
                DataColumn imagePathColumn = dataTable.Columns["ImagePath"];
                foreach (DataRow row in dataTable.Rows)
                {
                    if (!(row[imagePathColumn] is DBNull))
                    {
                        string imagePath = row[imagePathColumn].ToString();
                        if (!string.IsNullOrWhiteSpace(imagePath))
                        {
                            Match imagePathMatch = Regex.Match(imagePath, @"(?i)\\(?<ServerName>[^\\]+)\\(?<DriveShare>[^\\]+\$)\\(?<LocalPath>[^.]*)\\(?<FileName>.+.tif)");
                            if (imagePathMatch.Success)
                            {
                                // Get server name
                                string currentServerName = string.Empty;
                                Group matchSearch = imagePathMatch.Groups["ServerName"];
                                if (matchSearch.Success)
                                {
                                    currentServerName = matchSearch.Value;
                                }
                                else
                                {
                                    errorMessages.Add($"Could not determine Server Name from Image Path {imagePath}");
                                }
                                // Get drive
                                string currentDriveShare = string.Empty;
                                matchSearch = imagePathMatch.Groups["DriveShare"];
                                if (matchSearch.Success)
                                {
                                    currentDriveShare = matchSearch.Value;
                                }
                                else
                                {
                                    errorMessages.Add($"Could not determine Drive Share from Image Path {imagePath}");
                                }
                                // Get path
                                string currentLocalPath = string.Empty;
                                matchSearch = imagePathMatch.Groups["LocalPath"];
                                if (matchSearch.Success)
                                {
                                    currentLocalPath = matchSearch.Value;
                                }
                                else
                                {
                                    errorMessages.Add($"Could not determine Local Path from Image Path {imagePath}");
                                }
                                // Get file name
                                string fileName = string.Empty;
                                matchSearch = imagePathMatch.Groups["FileName"];
                                if (matchSearch.Success)
                                {
                                    fileName = matchSearch.Value;
                                }
                                else
                                {
                                    errorMessages.Add($"Could not determine File Name from Image Path {imagePath}");
                                }
                                string newServerName = string.IsNullOrWhiteSpace(serverName) ? currentServerName : serverName;
                                string newDriveShare = string.IsNullOrWhiteSpace(driveShare) ? currentDriveShare : driveShare;
                                string newLocalPath = localPath ?? currentLocalPath;
                                if (string.IsNullOrWhiteSpace(newLocalPath))
                                {
                                    row[imagePathColumn] = $@"\\{serverName}\{driveShare}\{fileName}";
                                }
                                else
                                {
                                    row[imagePathColumn] = $@"\\{serverName}\{driveShare}\{newLocalPath}\{fileName}";
                                }
                            }
                        }
                        else
                        {
                            string keys = string.Empty;
                            DataColumn[] primaryKeyColumns = dataTable.PrimaryKey;
                            for (int i = 0; i < primaryKeyColumns.Length; i++)
                            {
                                keys += $"{primaryKeyColumns[i].ColumnName}: {row[primaryKeyColumns[i]]}";
                                if (i < primaryKeyColumns.Length - 1)
                                {
                                    keys += ", ";
                                }
                            }
                            errorMessages.Add($"Row ImagePath is Null or WhiteSpace for {keys}");
                        }
                    }
                }
            }
        }

        public void ReplaceImagePathFields(List<DataTable> dataTables, string serverName = "localhost", string driveShare = "i$", string localPath = null)
        {
            foreach (DataTable dataTable in dataTables)
            {
                ReplaceImagePathFields(dataTable, serverName, driveShare, localPath);
            }
        }
    }
}