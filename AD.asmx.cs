﻿using System;
using System.Web.Services;
using System.Diagnostics;
using System.Configuration;
using System.Web.Configuration;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Text.RegularExpressions;

namespace Frontend
{
    /// <summary>
    /// Summary description for AD Webservice
    /// </summary>
    [WebService(Name = "AD Web Service", Description = "AD Web Service developed by Johan Arwidmark", Namespace = "http://www.deploymentresearch.com")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // [System.Web.Script.Services.ScriptService]
    public class ConfigMgr : System.Web.Services.WebService
    {

        #region Web methods

        [WebMethod]
        public Boolean AddComputerToGroup(String ADGroupName, String OSDComputerName, String DomainController)
        {

            Trace.WriteLine(DateTime.Now + ": AddComputerToGroup: Starting Web Service");
            Trace.WriteLine(DateTime.Now + ": AddComputerToGroup: ADGroupName received was: " + ADGroupName);
            Trace.WriteLine(DateTime.Now + ": AddComputerToGroup: OSDComputerName received was: " + OSDComputerName);
            Trace.WriteLine(DateTime.Now + ": AddComputerToGroup: DomainController received was: " + DomainController);

            try
            {

                // Connect to Active Directory
                PrincipalContext AD = new PrincipalContext(ContextType.Domain, DomainController);

                string controller = AD.ConnectedServer;
                Trace.WriteLine(DateTime.Now + ": AddComputerToGroup: Connected to " + string.Format("Domain Controller: {0}", controller));

                ComputerPrincipal computer = ComputerPrincipal.FindByIdentity(AD, OSDComputerName);

                if (computer != null)
                {
                    Trace.WriteLine(DateTime.Now + ": AddComputerToGroup: " + OSDComputerName + " computer found in AD, continue.");


                    GroupPrincipal group = GroupPrincipal.FindByIdentity(AD, ADGroupName);

                    if (group != null)
                    {
                        Trace.WriteLine(DateTime.Now + ": AddComputerToGroup: " + ADGroupName + " group found in AD, continue.");

                        group.Members.Add(computer);
                        group.Save();

                        Trace.WriteLine(DateTime.Now + ": AddComputerToGroup: " + OSDComputerName + " computer added to " + ADGroupName + " group");

                        return true;
                        

                    }
                    else
                    {
                        Trace.WriteLine(DateTime.Now + ": AddComputerToGroup: " + ADGroupName + " group not found in AD");
                        return false;
                    }


                }
                else
                {
                    Trace.WriteLine(DateTime.Now + ": AddComputerToGroup: " + OSDComputerName + "Machine not found in AD");
                    return false;
                }

            }


            catch (Exception e)
            {
                Trace.WriteLine(DateTime.Now + ": AddComputerToGroup: Unhandled exception finding provider namespace on server " + e.ToString());
                return false;

            }

        }

        [WebMethod]
        public Boolean MoveComputerToOU(String MACHINEOBJECTOU, String OSDComputerName, String DomainController)
        {

            Trace.WriteLine(DateTime.Now + ": MoveComputerToOU: Starting Web Service");
            Trace.WriteLine(DateTime.Now + ": MoveComputerToOU: MACHINEOBJECTOU received was: " + MACHINEOBJECTOU);
            Trace.WriteLine(DateTime.Now + ": MoveComputerToOU: OSDComputerName received was: " + OSDComputerName);
            Trace.WriteLine(DateTime.Now + ": MoveComputerToOU: DomainController received was: " + DomainController);

            String CurrentOU = string.Empty;
            Trace.WriteLine(DateTime.Now + ": MoveComputerToOU: Connecting to " + DomainController + ".");

            try
            {

                // Connect to AD
                PrincipalContext AD = new PrincipalContext(ContextType.Domain, DomainController);

                string controller = AD.ConnectedServer;
                Trace.WriteLine(DateTime.Now + ": MoveComputerToOU: Connected to " + string.Format("Domain Controller: {0}", controller));

                ComputerPrincipal computer = ComputerPrincipal.FindByIdentity(AD, OSDComputerName);

                if (computer != null)
                {
                    Trace.WriteLine(DateTime.Now + ": MoveComputerToOU: Machine found in AD, continue.");
                    // Get Parent OU 
                    DirectoryEntry deComputer = computer.GetUnderlyingObject() as DirectoryEntry;
                    DirectoryEntry deComputerContainer = deComputer.Parent;

                    CurrentOU = string.Format("{0}".Trim(), deComputerContainer.Properties["distinguishedName"].Value);

                    Trace.WriteLine(DateTime.Now + ": MoveComputerToOU: CurrentOU is " + CurrentOU);

                    // Verify if the selected OU is the same as the current OU
                    if (String.Equals(MACHINEOBJECTOU, CurrentOU, StringComparison.OrdinalIgnoreCase))
                    {
                        Trace.WriteLine(DateTime.Now + ": MoveComputerToOU: Selected OU is the same as current OU, or machine does not exist in AD, do nothing ");
                    }
                    else
                    {
                        Trace.WriteLine(DateTime.Now + ": MoveComputerToOU: Selected OU is not the same as currentOU, move computer to selected OU ");

                        // Move the computer object
                        DirectoryEntry NewParent = new DirectoryEntry("LDAP://" + MACHINEOBJECTOU);
                        deComputer.MoveTo(NewParent);
                    }
                }
                else
                {
                    Trace.WriteLine(DateTime.Now + ": MoveComputerToOU: Machine not found in AD, assuming new machine, skipping move operation.");
                }

            }


            catch (Exception e)
            {
                Trace.WriteLine(DateTime.Now + ": MoveComputerToOU: Unhandled exception finding provider namespace on server " + e.ToString());
                return false;

            }


            return true;

        }


        [WebMethod]

        public string GetComputerName(String SerialNumber)
        {
            string sGetComputerName;

            Trace.WriteLine(DateTime.Now + ": GetComputerName: Serial number is " + SerialNumber);

            if (SerialNumber.Length == 0)
            {

                Trace.WriteLine(DateTime.Now + ": GetComputerName: Serial number empty!");

                return "ERROR";

            }


            //Remove NetBIOS disallowed characters
            sGetComputerName = Regex.Replace(SerialNumber, @"(^\.)|([^a-zA-Z0-9\.-])", "", RegexOptions.None);

            Trace.WriteLine(DateTime.Now + ": GetComputerName: Removed illegal characters " + sGetComputerName);

            if (sGetComputerName.Length > 15)
            {
                sGetComputerName = sGetComputerName.Substring(0, 15);

                Trace.WriteLine(DateTime.Now + ": GetComputerName: Limited to 15 characters " + sGetComputerName);

            }

            Trace.WriteLine(DateTime.Now + ": GetComputerName: Calculated computer name is " + sGetComputerName);

            return sGetComputerName;

        }

        #endregion
    }
}
