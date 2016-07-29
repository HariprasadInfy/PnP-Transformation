﻿using JDP.Remediation.Console.Common.Base;
using JDP.Remediation.Console.Common.CSV;
using JDP.Remediation.Console.Common.Utilities;
using JDP.Remediation.Console;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.WebParts;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeDevPnP.Core.Utilities;
using OfficeDevPnP.Core.Entities;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using JDP.Remediation.Console.WebPartPagesService;


namespace JDP.Remediation.Console
{
    public class WebPartProperties
    {
        public static string filePath = string.Empty;
        public static string outputPath = string.Empty;
        public static void DoWork()
        {
            outputPath = Environment.CurrentDirectory;
            string webUrl = string.Empty;
            string serverRelativePageUrl = string.Empty;
            string webPartID = string.Empty;

            //Trace Log TXT File Creation Command
            Logger.OpenLog("WebpartProperties");

            System.Console.WriteLine("Please enter Web Url : ");
            webUrl = System.Console.ReadLine().ToLower();
            if (string.IsNullOrEmpty(webUrl))
            {
                Logger.LogErrorMessage("[WebpartProperties: DoWork]WebUrl should not be empty or null. Operation aborted...", true);
                return;
            }

            System.Console.WriteLine("Please enter Server Relative PageUrl (E:g- /sites/DTTesting/SitePages/WebPartPage.aspx): ");
            serverRelativePageUrl = System.Console.ReadLine().ToLower();
            if (string.IsNullOrEmpty(serverRelativePageUrl))
            {
                Logger.LogErrorMessage("[WebpartProperties: DoWork]ServerRelative PageUrl should not be empty or null. Operation aborted...", true);
                return;
            }

            System.Console.WriteLine("Please enter WebPart ID : ");
            webPartID = System.Console.ReadLine().ToLower();
            if (string.IsNullOrEmpty(webPartID))
            {
                Logger.LogErrorMessage("[WebpartProperties: DoWork]WebPart ID should not be empty or null. Operation aborted...", true);
                return;
            }
            Logger.LogInfoMessage(String.Format("Process started {0}", DateTime.Now.ToString()), true);
            try
            {
                GetWebPartProperties(serverRelativePageUrl, webPartID, webUrl, outputPath);
            }
            catch (Exception ex)
            {
                System.Console.ForegroundColor = System.ConsoleColor.Red;
                Logger.LogErrorMessage("[WebpartProperties: DoWork]. Exception Message: " + ex.Message, true);
                System.Console.ResetColor();
            }
            Logger.LogInfoMessage(String.Format("Process completed {0}", DateTime.Now.ToString()), true);
            Logger.CloseLog();
        }

        public static string GetWebPartProperties(string pageUrl, string StorageKey, string webUrl, string outPutDirectory)
        {
            string webPartPropertiesFileName = string.Empty;

            ClientContext clientContext = new ClientContext(webUrl);

            string webPartXml = string.Empty;
            ExceptionCsv.WebUrl = webUrl;
            string exceptionCommentsInfo1 = string.Empty;
            Web web = null;

            try
            {
                string sourceWebPartXmlFilesDir = outPutDirectory + @"\" + Constants.SOURCE_WEBPART_XML_DIR;

                if (!System.IO.Directory.Exists(sourceWebPartXmlFilesDir))
                {
                    System.IO.Directory.CreateDirectory(sourceWebPartXmlFilesDir);
                }

                //Deleted the Web Part Usage File
                DeleteUsageFiles_WebPartHelper(sourceWebPartXmlFilesDir, StorageKey + "_" + Constants.WEBPART_PROPERTIES_FILENAME);

                //Prepare Exception Comments
                exceptionCommentsInfo1 = "Web Url: " + webUrl + ", Page Url: " + pageUrl + ", StorageKey: " + StorageKey;


                using (ClientContext userContext = Helper.CreateAuthenticatedUserContext(Program.AdminDomain, Program.AdminUsername, Program.AdminPassword, webUrl))
                {
                    web = userContext.Web;
                    userContext.Load(web);
                    userContext.ExecuteQuery();
                    clientContext = userContext;

                    Logger.LogInfoMessage("[GetWebPartProperties] Retrieving WebPart Properties for StorageKey: " + StorageKey.ToString() + " in the Page" + pageUrl);

                    var service = new WebPartPagesService.WebPartPagesWebService();
                    service.Url = clientContext.Web.Url + Constants.WEBPART_SERVICE;

                    Logger.LogInfoMessage("[GetWebPartProperties] Service Url used to retrieve WebPart Properties : " + service.Url);

                    service.PreAuthenticate = true;

                    service.Credentials = clientContext.Credentials;

                    //For Publishing Pages, Pass - WebPartID
                    //For SitePage or Team Site, Pass - StorageKey.ToGuid()
                    webPartXml = service.GetWebPart2(pageUrl, StorageKey.ToGuid(), Storage.Shared, SPWebServiceBehavior.Version3);

                    Logger.LogInfoMessage("[GetWebPartProperties] Successfully retreived Web Part Properties");

                    webPartPropertiesFileName = sourceWebPartXmlFilesDir + "\\" + StorageKey + "_" + Constants.WEBPART_PROPERTIES_FILENAME;

                    using (StreamWriter fsWebPartProperties = new StreamWriter(webPartPropertiesFileName))
                    {
                        fsWebPartProperties.WriteLine(webPartXml);
                        fsWebPartProperties.Flush();
                    }

                    Logger.LogInfoMessage("[GetWebPartProperties] WebPart Properties in xml format is exported to the file " + webPartPropertiesFileName);
                }

            }
            catch (Exception ex)
            {
                System.Console.ForegroundColor = System.ConsoleColor.Red;
                Logger.LogErrorMessage("[GetWebPartProperties] Exception Message: " + ex.Message + ", Exception Comment: " + exceptionCommentsInfo1);
                System.Console.ResetColor();
            }

            return webPartPropertiesFileName;
        }

        public static void DeleteUsageFiles_WebPartHelper(string outPutFolder, string fileName)
        {
            //Delete Usage File
            Logger.LogInfoMessage("[DATE TIME] " + Logger.CurrentDateTime());

            FileUtility.DeleteFiles(outPutFolder + @"\" + fileName);

            Logger.LogInfoMessage("[DeleteUsageFiles_WebPartHelper] Deleted the Usage file : " + fileName + " from : " + outPutFolder);

        }


    }
}