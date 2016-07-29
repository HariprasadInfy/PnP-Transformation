﻿using JDP.Remediation.Console.Common.Base;
using JDP.Remediation.Console.Common.CSV;
using JDP.Remediation.Console.Common.Utilities;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.WebParts;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JDP.Remediation.Console
{
    public class DeleteWebparts
    {
        public static string filePath = string.Empty;
        public static string outputPath = string.Empty;
        public static void DoWork()
        {
            outputPath = Environment.CurrentDirectory;
            string webPartsInputFile = string.Empty;
            string webpartType = string.Empty;
            IEnumerable<WebpartInput> objWPDInput;

            //Trace Log TXT File Creation Command
            Logger.OpenLog("DeleteWebparts");

            if (!ReadInputFile(ref webPartsInputFile))
            {
                System.Console.ForegroundColor = System.ConsoleColor.Red;
                Logger.LogErrorMessage("Webparts input file is not valid or available. So, Operation aborted!");
                Logger.LogErrorMessage("Please enter path like: E.g. C:\\<Working Directory>\\<InputFile>.csv");
                System.Console.ResetColor();
                return;
            }

            Logger.LogMessage("Please enter Webpart Type (enter 'all' to delete all webparts):");
            webpartType = System.Console.ReadLine().ToLower();

            try
            {
                string csvFile = outputPath + @"/" + Constants.DeleteWebpartStatus;
                if (System.IO.File.Exists(csvFile))
                    System.IO.File.Delete(csvFile);
                if (String.Equals(Constants.WebpartType_All, webpartType, StringComparison.CurrentCultureIgnoreCase))
                {
                    //Reading Input File
                    objWPDInput = ImportCSV.ReadMatchingColumns<WebpartInput>(webPartsInputFile, Constants.CsvDelimeter);

                    if (objWPDInput.Any())
                    {
                        IEnumerable<string> webPartTypes = objWPDInput.Select(x => x.WebPartType);

                        webPartTypes = webPartTypes.Distinct();

                        foreach (string webPartType in webPartTypes)
                        {
                            try
                            {
                                DeleteWebPart_UsingCSV(webPartType, webPartsInputFile, csvFile);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogErrorMessage("[DeleteMissingWebparts: DoWork]. Exception Message: " + ex.Message + ", Exception Comments: ", true);
                            }
                        }
                        webPartTypes = null;
                    }
                }
                else
                {
                    DeleteWebPart_UsingCSV(webpartType, webPartsInputFile, csvFile);
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DeleteMissingWebparts: DoWork]. Exception Message: " + ex.Message, true);
            }
            finally
            {
                objWPDInput = null;
            }
            System.Console.ForegroundColor = System.ConsoleColor.Yellow;
            Logger.LogSuccessMessage("Processing Webparts input file is processed...");
            System.Console.ResetColor();
            Logger.CloseLog();
        }

        public static void DeleteWebPart_UsingCSV(string webPartType, string webPartsInputFile, string csvFile)
        {
            string exceptionCommentsInfo1 = string.Empty;

            try
            {
                //Reading Input File
                IEnumerable<WebpartInput> objWPDInput;
                ReadWebPartUsageCSV(webPartType, webPartsInputFile, out objWPDInput);

                bool headerTransformWebPart = false;

                if (objWPDInput.Any())
                {
                    for (int i = 0; i < objWPDInput.Count(); i++)
                    {
                        WebpartInput objInput = objWPDInput.ElementAt(i);
                        WebpartDeleteOutputBase objWPOutputBase = new WebpartDeleteOutputBase();

                        try
                        {
                            bool status = DeleteWebPart(objInput.WebUrl, objInput.PageUrl.ToString(), objInput.StorageKey);

                            if (status)
                            {
                                objWPOutputBase.Status = "Success";
                                System.Console.ForegroundColor = System.ConsoleColor.Green;
                                Logger.LogInfoMessage("[DeleteMissingWebparts: DeleteWebPart_UsingCSV]Successfully Deleted WebPart with Webpart Type " + objInput.WebPartType + " and with StorageKey " + objInput.StorageKey);
                                System.Console.ResetColor();
                            }
                            else
                            {
                                objWPOutputBase.Status = "Failed";
                                System.Console.ForegroundColor = System.ConsoleColor.Gray;
                                Logger.LogInfoMessage("[DeleteMissingWebparts: DeleteWebPart_UsingCSV]Failed to Delete WebPart with Webpart Type " + objInput.WebPartType + " and with StorageKey " + objInput.StorageKey);
                                System.Console.ResetColor();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogInfoMessage("[DeleteMissingWebparts: DeleteWebPart_UsingCSV]Failed to Deleted WebPart with Webpart Type " + objInput.WebPartType + " and with StorageKey " + objInput.StorageKey);
                            Logger.LogErrorMessage("[DeleteMissingWebparts: DeleteWebPart_UsingCSV]. Exception Message: " + ex.Message, true);
                        }

                        objWPOutputBase.WebPartType = objInput.WebPartType;
                        objWPOutputBase.PageUrl = objInput.PageUrl;
                        objWPOutputBase.WebUrl = objInput.WebUrl;
                        objWPOutputBase.StorageKey = objInput.StorageKey;

                        if (System.IO.File.Exists(csvFile))
                        {
                            headerTransformWebPart = true;
                        }
                        FileUtility.WriteCsVintoFile(csvFile, objWPOutputBase, ref headerTransformWebPart);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DeleteMissingWebparts: DeleteWebPart_UsingCSV]. Exception Message: " + ex.Message + ", Exception Comments: ", true);
            }
        }

        private static void ReadWebPartUsageCSV(string sourceWebPartType, string usageFilePath, out IEnumerable<WebpartInput> objWPDInput)
        {
            objWPDInput = null;
            objWPDInput = ImportCSV.ReadMatchingColumns<WebpartInput>(usageFilePath, Constants.CsvDelimeter);

            try
            {
                if (objWPDInput.Any())
                {
                    objWPDInput = from p in objWPDInput
                                  where p.WebPartType.Equals(sourceWebPartType, StringComparison.OrdinalIgnoreCase)
                                  select p;

                    if (objWPDInput.Any())
                        Logger.LogInfoMessage("[DeleteMissingWebparts: ReadWebPartUsageCSV]Number of Webparts found with WebpartType '" + sourceWebPartType + "' are " + objWPDInput.Count());
                    else
                        Logger.LogInfoMessage("[DeleteMissingWebparts: ReadWebPartUsageCSV]No Webparts found with WebpartType '" + sourceWebPartType + "'");
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DeleteMissingWebparts: DoWork]. Exception Message: " + ex.Message
                    + ", Exception Comments: Exception occured while rading input file ", true);
            }
        }

        public static bool DeleteWebPart(string webUrl, string pageUrl, string _storageKey)
        {
            bool isWebPartDeleted = false;
            string webPartXml = string.Empty;
            string exceptionCommentsInfo1 = string.Empty;
            Web web = null;
            List list = null;

            try
            {
                //This function is Get Relative URL of the page
                string ServerRelativePageUrl = string.Empty;
                ServerRelativePageUrl = GetPageRelativeURL(webUrl, pageUrl);

                Guid storageKey = new Guid(GetWebPartID(_storageKey));

                using (ClientContext userContext = Helper.CreateAuthenticatedUserContext(Program.AdminDomain, Program.AdminUsername, Program.AdminPassword, webUrl))
                {
                    web = userContext.Web;
                    userContext.Load(web);
                    userContext.ExecuteQuery();

                    Logger.LogInfoMessage("[DeleteMissingWebparts:DeleteWebPart] Successful authentication", false);

                    Logger.LogInfoMessage("[DeleteMissingWebparts:DeleteWebPart] Checking Out File ...", false);

                    list = GetPageList(userContext);

                    //Boolean to check if a call to Update method is required
                    bool needsUpdate = false;
                    bool forceCheckOut = false;
                    bool enableVersioning = false;
                    bool enableMinorVersions = false;
                    bool enableModeration = false;
                    DraftVisibilityType dVisibility = DraftVisibilityType.Author;

                    if (list != null)
                    {
                        try
                        {
                            userContext.Load(list, l => l.ForceCheckout,
                                       l => l.EnableVersioning,
                                       l => l.EnableMinorVersions,
                                       l => l.EnableModeration,
                                       l => l.Title,
                                       l => l.DraftVersionVisibility,
                                       l => l.DefaultViewUrl);

                            userContext.ExecuteQueryRetry();

                            #region Remove Versioning in List
                            forceCheckOut = list.ForceCheckout;
                            enableVersioning = list.EnableVersioning;
                            enableMinorVersions = list.EnableMinorVersions;
                            enableModeration = list.EnableModeration;
                            dVisibility = list.DraftVersionVisibility;

                            Logger.LogInfoMessage("[DeleteMissingWebparts:DeleteWebpart] Removing Versioning", false);
                            //Boolean to check if a call to Update method is required
                            needsUpdate = false;

                            if (enableVersioning)
                            {
                                list.EnableVersioning = false;
                                needsUpdate = true;
                            }
                            if (forceCheckOut)
                            {
                                list.ForceCheckout = false;
                                needsUpdate = true;
                            }
                            if (enableModeration)
                            {
                                list.EnableModeration = false;
                                needsUpdate = true;
                            }

                            if (needsUpdate)
                            {
                                list.Update();
                                userContext.ExecuteQuery();
                            }
                            #endregion
                        }
                        catch (Exception ex)
                        {
                            Logger.LogErrorMessage("[DeleteMissingWebparts: DoWork]. Exception Message: " + ex.Message
                                + ", Exception Comments: Exception while removing Version to the list", true);
                        }
                    }

                    try
                    {
                        if (DeleteWebPart(userContext.Web, ServerRelativePageUrl, storageKey))
                        {
                            isWebPartDeleted = true;
                            Logger.LogInfoMessage("[DeleteMissingWebparts:DeleteWebPart] Successfully Deleted the WebPart", false);
                        }
                        else
                        {
                            Logger.LogInfoMessage("[DeleteMissingWebparts:DeleteWebPart] WebPart with StorageKey: " + storageKey + " does not exist in the Page: " + ServerRelativePageUrl, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogErrorMessage("[DeleteMissingWebparts: DoWork]. Exception Message: " + ex.Message + ", Exception Comments: ", true);
                    }
                    finally
                    {
                        if (list != null)
                        {
                            #region Enable Versioning in List
                            //Reset the boolean so that it can used to test if we need to call Update method
                            needsUpdate = false;
                            if (enableVersioning)
                            {
                                list.EnableVersioning = true;
                                if (enableMinorVersions)
                                {
                                    list.EnableMinorVersions = true;
                                }
                                if (enableMinorVersions)
                                {
                                    list.EnableMinorVersions = true;
                                }

                                list.DraftVersionVisibility = dVisibility;
                                needsUpdate = true;
                            }
                            if (enableModeration)
                            {
                                list.EnableModeration = enableModeration;
                                needsUpdate = true;
                            }
                            if (forceCheckOut)
                            {
                                list.ForceCheckout = true;
                                needsUpdate = true;
                            }
                            if (needsUpdate)
                            {
                                list.Update();
                                userContext.ExecuteQuery();
                            }
                            #endregion
                        }
                        web = null;
                        list = null;
                    }
                    Logger.LogInfoMessage("[DeleteMissingWebparts:DeleteWebPart]  File Checked in after successfully deleting the webpart.", false);
                }

            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DeleteMissingWebparts: DoWork]. Exception Message: ", true);
            }
            return isWebPartDeleted;
        }

        private static bool DeleteWebPart(Web web, string serverRelativePageUrl, Guid storageKey)
        {
            bool isWebPartDeleted = false;
            LimitedWebPartManager limitedWebPartManager = null;
            try
            {
                var webPartPage = web.GetFileByServerRelativeUrl(serverRelativePageUrl);

                web.Context.Load(webPartPage);
                web.Context.ExecuteQueryRetry();

                limitedWebPartManager = webPartPage.GetLimitedWebPartManager(PersonalizationScope.Shared);
                web.Context.Load(limitedWebPartManager.WebParts, wps => wps.Include(wp => wp.Id));
                web.Context.ExecuteQueryRetry();

                if (limitedWebPartManager.WebParts.Count >= 0)
                {
                    foreach (WebPartDefinition webpartDef in limitedWebPartManager.WebParts)
                    {
                        Microsoft.SharePoint.Client.WebParts.WebPart oWebPart = null;
                        try
                        {
                            oWebPart = webpartDef.WebPart;
                            if (webpartDef.Id.Equals(storageKey))
                            {
                                webpartDef.DeleteWebPart();
                                web.Context.ExecuteQueryRetry();
                                isWebPartDeleted = true;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogErrorMessage("[DeleteMissingWebparts: DoWork]. Exception Message: " + ex.Message
                                + ", Exception Comments: Exception occured while deleting the webpart", true);
                        }
                        finally
                        {
                            oWebPart = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DeleteMissingWebparts: DoWork]. Exception Message: "
                    + ex.Message + ", Exception Comments: Exception occure while fetching webparts using LimitedWebPartManager", true);
            }
            finally
            {
                limitedWebPartManager = null;
            }
            return isWebPartDeleted;
        }

        private static string GetPageRelativeURL(string WebUrl, string PageUrl, string UserName = "N/A", string Password = "N/A", string Domain = "N/A")
        {
            string _relativePageUrl = string.Empty;
            try
            {
                if (WebUrl != "" || PageUrl != "")
                {
                    using (ClientContext userContext = Helper.CreateAuthenticatedUserContext(Program.AdminDomain, Program.AdminUsername, Program.AdminPassword, WebUrl))
                    {
                        Web _Web = userContext.Web;
                        userContext.Load(_Web);
                        userContext.ExecuteQuery();

                        Logger.LogInfoMessage("[DeleteMissingWebparts: GetPageRelativeURL] Web.ServerRelativeUrl: " + _Web.ServerRelativeUrl + " And PageUrl: " + PageUrl, true);

                        //Issue: Found in MARS Retraction Process, the root web ServerRelativeUrl would result "/" only
                        //Hence appending "/" would throw exception for ServerRelativeUrl parameter
                        if (_Web.ServerRelativeUrl.ToString().Equals("/"))
                        {
                            _relativePageUrl = _Web.ServerRelativeUrl.ToString() + PageUrl;
                        }
                        else if (!PageUrl.Contains(_Web.ServerRelativeUrl))
                        {
                            _relativePageUrl = _Web.ServerRelativeUrl.ToString() + "/" + PageUrl;
                        }
                        else
                        {
                            _relativePageUrl = PageUrl;
                        }
                        Logger.LogInfoMessage("[DeleteMissingWebparts: GetPageRelativeURL] RelativePageUrl Framed: " + _relativePageUrl, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DeleteMissingWebparts: GetPageRelativeURL]. Exception Message: " + ex.Message
                    + ", Exception Comments: Exception occured while reading page relive url", true);
            }

            return _relativePageUrl;
        }

        private static string GetWebPartID(string webPartID)
        {
            string _webPartID = string.Empty;

            try
            {
                string[] tempStr = webPartID.Split('_');

                if (tempStr.Length > 5)
                {
                    _webPartID = webPartID.Remove(0, tempStr[0].Length + 1).Replace('_', '-');
                }
                else
                {
                    _webPartID = webPartID.Replace('_', '-');
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DeleteMissingWebparts: DoWork]. Exception Message: " + ex.Message, true);
            }
            return _webPartID;
        }

        private static List GetPageList(ClientContext clientContext)
        {
            List list = null;
            Web web = null;
            try
            {
                web = clientContext.Web;

                // Get a few properties from the web
                clientContext.Load(web,
                                    w => w.Url,
                                    w => w.ServerRelativeUrl,
                                    w => w.AllProperties,
                                    w => w.WebTemplate);

                clientContext.ExecuteQueryRetry();

                string pagesListID = string.Empty;
                bool _IsPublishingWeb = IsPublishingWeb(clientContext, web);

                if (_IsPublishingWeb)
                {
                    Logger.LogInfoMessage("[DeleteMissingWebparts:GetPageList]Web: " + web.Url + "is a publishing web", false);
                    pagesListID = web.AllProperties["__PagesListId"] as string;
                    list = web.Lists.GetById(new Guid(pagesListID));


                    clientContext.Load(list, l => l.ForceCheckout,
                                       l => l.EnableVersioning,
                                       l => l.EnableMinorVersions,
                                       l => l.EnableModeration,
                                       l => l.Title,
                                       l => l.DraftVersionVisibility,
                                       l => l.DefaultViewUrl);

                    clientContext.ExecuteQueryRetry();

                }
                else
                {
                    Logger.LogInfoMessage("[DeleteMissingWebparts:GetPageList]Web: " + web.Url + "is not a publishing web", false);
                    clientContext.Load(web.Lists);

                    clientContext.ExecuteQueryRetry();

                    try
                    {
                        //list = web.Lists.GetByTitle(Constants.TEAMSITE_PAGES_LIBRARY);
                        //WebPageLibrary, Wiki Page Library. Value = 119.
                        IEnumerable<List> libraries = clientContext.LoadQuery(web.Lists.Where(l => l.BaseTemplate == 119));
                        clientContext.ExecuteQuery();

                        if (libraries.Any() && libraries.Count() > 0)
                        {
                            list = libraries.First();
                        }

                        clientContext.Load(list);
                        clientContext.ExecuteQueryRetry();
                    }
                    catch
                    {
                        list = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DeleteMissingWebparts: GetPageList]. Exception Message: " + ex.Message
                    + ", Exception Comments: Exception occured while finding page list", true);
            }
            finally
            {
                clientContext.Dispose();
                web = null;
            }
            return list;
        }

        private static bool IsPublishingWeb(ClientContext clientContext, Web web)
        {
            Logger.LogInfoMessage("[DeleteMissingWebparts:IsPublishingWeb] Checking if the current web is a publishing web", false);

            var _IsPublished = false;
            var propName = "__PublishingFeatureActivated";

            try
            {

                //Ensure web properties are loaded
                if (!web.IsObjectPropertyInstantiated("AllProperties"))
                {
                    clientContext.Load(web, w => w.AllProperties);
                    clientContext.ExecuteQuery();
                }
                //Verify whether publishing feature is activated 
                if (web.AllProperties.FieldValues.ContainsKey(propName))
                {
                    bool propVal;
                    Boolean.TryParse((string)web.AllProperties[propName], out propVal);
                    _IsPublished = propVal;
                    return propVal;
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage("[DeleteMissingWebparts: DoWork]. Exception Message: "
                    + ex.Message + ", Exception Comments: Exception occured while finding publishing page", true);
            }
            finally
            {
                clientContext.Dispose();
                web = null;
            }
            return _IsPublished;
        }

        private static bool ReadInputFile(ref string webPartsInputFile)
        {
            Logger.LogMessage("Enter Complete Input File Path of Webparts Report Either Pre-Scan OR Discovery Report:");
            webPartsInputFile = System.Console.ReadLine();
            Logger.LogMessage("[DownloadAndModifyListTemplate: ReadInputFile] Entered Input File of List Template Data " + webPartsInputFile, false);
            if (string.IsNullOrEmpty(webPartsInputFile) || !System.IO.File.Exists(webPartsInputFile))
                return false;
            return true;
        }
    }
}
