using System;
using System.Collections.Generic;
using System.Text;
using Contensive.BaseClasses;
using System.Collections;

namespace aoForums2
{
    //
    // 1) Change the namespace to the collection name
    // 2) Change this class name to the addon name
    // 3) Create a Contensive Addon record with the namespace apCollectionName.ad
    //
    public class sendDailyUpdatesClass : Contensive.BaseClasses.AddonBaseClass
    {
        public const string sendUpdatesGuid = "{D327F40F-F968-4090-89AD-89621627EBA0}";
        //
        // execute method is the only public
        //
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp)
        {
            string returnHtml = "";
            DateTime dateLastEmail;
            DateTime rightNow = DateTime.Now ;
            DateTime today = rightNow.Date;
            DateTime yesterday = today.AddDays(-1);
            //string sqlCriteria = "";
            string sqlDateLastEmail = "";
            //
            try
            {
                dateLastEmail = cp.Site.GetDate("Forums Notification Last Sent", yesterday.ToString() );
                sqlDateLastEmail = cp.Db.EncodeSQLDate(dateLastEmail);
                if (dateLastEmail < today)
                {
                    cp.Utils.ExecuteAddon(sendUpdatesGuid);
                }
            }
            catch (Exception ex)
            {
                errorReport(cp, ex, "execute");
            }
            return returnHtml;
        }
        //
        // ===============================================================================
        // handle errors for this class
        // ===============================================================================
        //
        private void errorReport(CPBaseClass cp, Exception ex, string method)
        {
            cp.Site.ErrorReport(ex, "error in aoForums2.sendDailyUpdatesClass." + method);
        }
    }
}
