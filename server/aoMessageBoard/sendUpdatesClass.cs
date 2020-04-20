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
    public class sendUpdatesClass : Contensive.BaseClasses.AddonBaseClass
    {
        //
        // execute method is the only public
        //
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp)
        {
            const string forumNotificationBody = ""
                + "<h2>Forum Notification</h2>"
                + "<p>The following forums have had changes over the past day.<p>"
                + "";
            string returnHtml = "";
            DateTime dateLastEmail;
            DateTime rightNow = DateTime.Now ;
            DateTime today = rightNow.Date;
            DateTime yesterday = today.AddDays(-1);
            CPCSBaseClass cs = cp.CSNew();
            int memberId;
            bool memberMatch;
            string sqlCriteria = "";
            string forumIdChangedList = "";
            int forumId;
            Hashtable forumNamesRef = new Hashtable();
            string emailBody = "";
            string sql;
            string testSrc;
            int testId = 0;
            string qs = "";
            string forumQs = cp.Site.GetText("forum last display qs", "");
            string sqlDateLastEmail = "";
            string emailDomain = cp.Site.DomainPrimary;
            //
            try
            {
                dateLastEmail = cp.Site.GetDate("Forums Notification Last Sent", yesterday.ToString() );
                cp.Site.SetProperty("Forums Notification Last Sent", rightNow.ToString());
                sqlDateLastEmail = cp.Db.EncodeSQLDate(dateLastEmail);
                //
                // verify Forum Notification email
                //
                testId = cp.Content.GetRecordID("system email", "Forum Notification");
                if (testId == 0)
                {
                    if (emailDomain.IndexOf(".") < 0)
                    {
                        emailDomain = "kma.net";
                    }
                    cs.Insert("system Email");
                    cs.SetField("name", "Forum Notification");
                    cs.SetField("subject", cp.Site.DomainPrimary + " Daily Forum Updates");
                    cs.SetField("fromAddress", "ForumNotification@" + emailDomain);
                    cs.SetField("copyFilename", forumNotificationBody);
                    cs.Close();
                }
                //
                // make a list of forums with changes
                //
                sql = "select distinct f.id as forumId,f.name as forumName"
                    + " from ((ccForums f"
                    + " left join ccforumThreads t on t.forumId=f.id)"
                    + " left join ccforumPosts p on p.threadid=t.id)"
                    + " where"
                    + " (t.dateAdded>" + sqlDateLastEmail + ")"
                    + " or(p.dateAdded>" + sqlDateLastEmail + ")"
                    + " order by f.id";
                cs.OpenSQL(sql);
                while (cs.OK())
                {
                    forumId = cs.GetInteger("forumId");
                    testSrc = "," + forumIdChangedList + ",";
                    if (testSrc.IndexOf("," + forumId.ToString() + ",") < 0)
                    {
                        forumIdChangedList += "," + forumId;
                        sqlCriteria += "or(forumId=" + forumId + ")";
                        forumNamesRef.Add(forumId, cs.GetText("forumName"));
                    }
                    cs.GoNext();
                }
                cs.Close();
                //
                // check for people who want notifications for these forums
                //
                if (sqlCriteria != "")
                {
                    sqlCriteria = "(" + sqlCriteria.Substring(2) + ")";
                    if (cs.Open("forum notification rules", sqlCriteria, "memberId,forumId", true, "memberId,forumId", 999, 1))
                    {
                        do
                        {
                            //
                            // send this member a list of forums that changed and are on his list
                            //
                            memberId = cs.GetInteger("memberId");
                            emailBody = "";
                            do
                            {
                                memberMatch = (memberId == cs.GetInteger("memberId"));
                                if (memberMatch)
                                {
                                    forumId = cs.GetInteger("forumId");
                                    testSrc = "," + forumIdChangedList + ",";
                                    if (testSrc.IndexOf("," + forumIdChangedList.ToString() + ",") >= 0)
                                    {
                                        qs = cp.Utils.ModifyQueryString(forumQs, "forumId", forumId.ToString(), true);
                                        emailBody += "<li><a href=\"http://" + cp.Site.DomainPrimary + cp.Site.AppRootPath + cp.Site.PageDefault + "?" + qs + "\">" + forumNamesRef[forumId].ToString() + "</a></li>";
                                    }
                                    cs.GoNext();
                                }
                            }
                            while (cs.OK() && memberMatch);
                            if (emailBody != "")
                            {
                                emailBody = "<ul>" + emailBody + "</ul>";
                                cp.Email.sendSystem("Forum Notification", emailBody, memberId);
                            }
                        }
                        while (cs.OK());
                    }
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
            cp.Site.ErrorReport(ex, "error in aoForums2.sendUpdatesClass." + method);
        }
    }
}
