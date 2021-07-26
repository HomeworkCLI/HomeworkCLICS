using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HomeworkCLI
{
    public class HomeworkCLICore
    {
        private static readonly WebClient webClient = new WebClient();
        public string Userid { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string SchoolId { get; set; } = string.Empty;
        public string CycoreId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "http://www.yixuexiao.cn/";
        public string Mac { get; set; } = "07:91:2B:0D:55:8B";
        public string Machine { get; set; } = "Google Pixel 3 XL";
        public string OsVersion { get; set; } = "11.0";

        public Task<JObject> ClientLogin(string username, string password, bool isforce, int usertype = 1)
        {
            return this.Post(
                Urls.clientLogin,
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "loginvalue", username },
                { "pwd", EncryptDES(password) },
                { "device", "mobile" },
                { "isforce", isforce.ToString().ToLower() },
                { "usertype", usertype.ToString() },
                { "appVersion", "v3.8.9.4" }
            })).ContinueWith(antecendent =>
            {
                if (antecendent.Result.Value<int>("code") == 1)
                {
                    JObject data = antecendent.Result.Value<JObject>("data");
                    this.Userid = data.Value<string>("id");
                    this.Token = data.Value<string>("token");
                    this.SchoolId = data.Value<string>("schoolId");
                    this.CycoreId = data.Value<string>("cycoreId");
                    this.DisplayName = data.Value<string>("displayName");
                }
                return antecendent.Result;
            });
        }

        public void ClientLoginWithJSON(string json)
        {
            JObject data = JObject.Parse(json).Value<JObject>("data");
            this.Userid = data.Value<string>("id");
            this.Token = data.Value<string>("token");
            this.SchoolId = data.Value<string>("schoolId");
            this.CycoreId = data.Value<string>("cycoreId");
            this.DisplayName = data.Value<string>("displayName");
        }
        public void ClientLoginWithJSON(JObject json)
        {
            JObject data = json.Value<JObject>("data");
            this.Userid = data.Value<string>("id");
            this.Token = data.Value<string>("token");
            this.SchoolId = data.Value<string>("schoolId");
            this.CycoreId = data.Value<string>("cycoreId");
            this.DisplayName = data.Value<string>("displayName");
        }

        public Task<JObject> CoursewareList(string page = "1")
        {
            return this.Post(
                Urls.coursewarelist,
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "page", page },
                { "userid", this.Userid }
            }));
        }

        public Task<JObject> MycoursewareList(string page = "1")
        {
            return this.Post(
                Urls.mycoursewarelist,
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "page", page },
                { "userid", this.Userid }
            }));
        }
        public Task<JObject> GetStudentDialogList()
        {
            return this.Post(
                Urls.getStudentDialogList,
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "studentid", this.Userid }
            }));
        }

        public Task<JObject> GetNoticeList(string page = "1")
        {
            return this.Post(
                Urls.getNoticeList,
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "page", page },
                { "userid", this.Userid }
            }));
        }
        public Task<JObject> GetNewMessage()
        {
            return this.Post(
                Urls.getNewMessage,
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "userid", this.Userid }
            }));
        }
        public Task<JObject> GetUserRank()
        {
            return this.Post(
                Urls.getUserRank,
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "userid", this.Userid }
            }));
        }
        public Task<JObject> GetReadandCommentCount(string noticeId)
        {
            return this.Post(
                Urls.getReadandCommentCount,
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "noticeId", noticeId }
            }));
        }
        public Task<JObject> CheckModuleStatus(string moduleid)
        {
            return this.Post(
                Urls.getReadandCommentCount,
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "userid", this.Userid },
                { "moduleid", moduleid }
            }));
        }
        public Task<JObject> TodaySign()
        {
            return this.Post(
                Urls.todaySign,
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "userid", this.Userid }
            }));
        }
        public Task<JObject> OperateLesson(string lessonDynamicId, string lessonId, string type)
        {
            return this.Post(
                this.BaseUrl + "jcservice/lesson/operateLesson",
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "lessonDynamicId", lessonDynamicId },
                { "lessonId", lessonId },
                { "type", type },
                { "userid", this.Userid }
            }));
        }
        public Task<JObject> OperateLessonInfo(string lessonDynamicId, string lessonId)
        {
            return this.Post(
                this.BaseUrl + "jcservice/lesson/operateLessonInfo",
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "lessonDynamicId", lessonDynamicId },
                { "lessonId", lessonId },
                { "userid", this.Userid }
            }));
        }
        public Task<JObject> AddComment(string lessonDynamicId, string lessonId, string comment, string commentType, string commentId)
        {
            return this.Post(
                Urls.addComment,
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "lessonDynamicId", lessonDynamicId },
                { "lessonId", lessonId },
                { "userid", this.Userid }
            }));
        }
        public Task<JObject> SetCoursewareInfo(string dynamicId, string type)
        {
            return this.Post(
                Urls.setCoursewareInfo,
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "dynamicId", dynamicId },
                { "type", type },
                { "userid", this.Userid }
            }));
        }
        public Task<JObject> CoursewareDynamicData(string dynamicId)
        {
            return this.Post(
                Urls.coursewareDynamicData,
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "dynamicId", dynamicId },
                { "userid", this.Userid }
            }));
        }
        public Task<JObject> ListStuLessonClass(string dynamicId, string pageSize = "10", string bankname = "", string keyword = "")
        {
            return this.Post(
                this.BaseUrl + "jcservice/lesson/listStuLessonClass",
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "dynamicId", dynamicId },
                { "pageSize", pageSize },
                { "bankname", bankname },
                { "keyword", keyword },
                { "userid", this.Userid }
            }));
        }
        public Task<JObject> GetChatRecordByStudent(string teacherid, string page)
        {
            return this.Post(
                this.BaseUrl + "/forum/FSNoticeHome-getMessageByUser",
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "studentid", this.Userid },
                { "teacherid", teacherid },
                { "page", page },
                { "userid", this.Userid }
            }));
        }

        public Task<JObject> SendChatMessage(string reUserid, string type, string content, string totaltime = "0")
        {
            return this.Post(
                Urls.sendChatMessage,
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "totalTime", totaltime },
                { "type", type },
                { "userid", this.Userid },
                { "content", content },
                { "reUserid", reUserid }
            }));
        }

        public Task<JObject> SaveDocNew(DocInfo docInfo)
        {
            return this.Post(
                this.BaseUrl + "jcservice/Doc/saveDocNew",
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "docInfoJson", JsonConvert.SerializeObject(docInfo) },
                { "appVersion", "v3.8.8.3" }
            }, false));
        }

        public Task<JObject> ShareDoc(string userfor, string classids, string docid, string studentids)
        {
            return this.Post(
                this.BaseUrl + "jcservice/Courseware/shareDoc",
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "userfor", userfor },
                { "classids", classids },
                { "docid", docid },
                { "studentids", studentids },
                { "userid", this.Userid },
                { "appVersion", "v3.8.8.3" }
            }, false));
        }

        public Task<JObject> CreateHomeWork(WorkInfo workInfo, string isareanet = "0", string draftid = "")
        {
            return this.Post(
                this.BaseUrl + "jcservice/TeaHomeWork/createHomeWork",
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "draftid", draftid },
                { "workjson", JsonConvert.SerializeObject(workInfo) },
                { "isareanet", isareanet },
            }));
        }

        public Task<JObject> GetChatRecordByTeacher(string studentid, string page)
        {
            return this.Post(
                this.BaseUrl + "/forum/FSNoticeHome-getMessageByUser",
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "teacherid", this.Userid },
                { "studentid", studentid },
                { "page", page },
                { "userid", this.Userid }
            }));
        }

        public Task<JObject> ListTeaShareDynamic(string pageSize = "10")
        {
            return this.Post(
                this.BaseUrl + "jcservice/Courseware/listTeaShareDynamic",
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "pagesize", pageSize },
                { "userid", this.Userid }
            }));
        }

        public Task<JObject> GetTeaDoc(string pageSize = "10", string title = "")
        {
            return this.Post(
                this.BaseUrl + "jcservice/courseware/getTeaDoc",
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "pagesize", pageSize },
                { "title", title },
                { "userid", this.Userid }
            }));
        }

        public Task<JObject> DelDynamicByTea(string shareId)
        {
            return this.Post(
                this.BaseUrl + "jcservice/Courseware/delDynamicByTea",
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "shareId", shareId }
            }));
        }
        public Task<JObject> DelCourseware(string docid)
        {
            return this.Post(
                this.BaseUrl + "jcservice/Courseware/delCourseware",
                this.EncryptFormData(new Dictionary<string, string>
            {
                { "docid", docid },
                { "userid", this.Userid}
            }));
        }
        public Task<JObject> getOssSecretKeyNew()
        {
            long timestamp = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
            StringBuilder stringBuilder = new StringBuilder();
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(this.Userid + "appId" + timestamp + "456FDB96EBB94035A926827139EA4216"));
                foreach (byte b in data)
                {
                    string tmp = (b & 0xff).ToString("x2");
                    if (tmp.Length == 1)
                    {
                        tmp = "0" + tmp;
                    }
                    stringBuilder.Append(tmp);
                }
            }
            return this.Post(
                Urls.getOssSecretKeyNew,
                new Dictionary<string, string>
                {
                    { "userId", this.Userid },
                    { "timestamp", timestamp.ToString() },
                    { "appId", "appId" },
                    { "sign", stringBuilder.ToString() }
                });
        }

        private Dictionary<string, string> EncryptFormData(Dictionary<string, string> formData, bool hasToken = true)
        {
            long timestamp = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
            formData.Add("safeid", this.Userid);
            formData.Add("safetime", timestamp.ToString());
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(this.Userid + timestamp + "jevictek.homework"));
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    stringBuilder.Append(data[i].ToString("x2"));
                }

                formData.Add("key", stringBuilder.ToString());
            }
            formData.Add("mac", this.Mac);
            formData.Add("machine", this.Machine);
            formData.Add("platform", "Android");
            formData.Add("osVersion", this.OsVersion);
            formData.Add("apiVersion", "1.0");
            if (!string.IsNullOrEmpty(this.Token) && hasToken)
            {
                formData.Add("token", this.Token);
            }
            return formData;
        }

        private static string EncryptDES(string encryptString)
        {
            try
            {
                byte[] key = Encoding.UTF8.GetBytes("jevicjob");
                byte[] inputByteArray = Encoding.UTF8.GetBytes(encryptString);
                MemoryStream mStream = new MemoryStream();
                DESCryptoServiceProvider dESCryptoServiceProvider = new DESCryptoServiceProvider
                {
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7
                };
                CryptoStream cStream = new CryptoStream(
                    mStream,
                    dESCryptoServiceProvider.CreateEncryptor(key, key),
                    CryptoStreamMode.Write);
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                cStream.FlushFinalBlock();
                cStream.Dispose();
                dESCryptoServiceProvider.Dispose();
                return Convert.ToBase64String(mStream.ToArray());
            }
            catch
            {
                return encryptString;
            }
        }

        private Task<JObject> Post(string url, Dictionary<string, string> content)
        {
            StringBuilder dictionaryStringBuilder = new StringBuilder();
            foreach (KeyValuePair<string, string> keyValues in content)
            {
                dictionaryStringBuilder.Append(keyValues.Key);
                dictionaryStringBuilder.Append('=');
                dictionaryStringBuilder.Append(keyValues.Value);
                dictionaryStringBuilder.Append('&');
            }
            webClient.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            return webClient.UploadDataTaskAsync(url, Encoding.UTF8.GetBytes(dictionaryStringBuilder.ToString().TrimEnd('&'))).ContinueWith(antecendent =>
            {
#if DEBUG
                Console.WriteLine(Encoding.UTF8.GetString(antecendent.Result));
#endif
                return JObject.Parse(Encoding.UTF8.GetString(antecendent.Result));
            });
        }
    }
    public class WorkInfo
    {

    }
    public class DocInfo
    {
        public string title;
        public string doctype;
        public long docsize;
        public string dir;
        public string key;
        public string md5code;
        public string guid;
        public bool isconverth5;
        public bool ispublish;
        public string agent;
        public string iflyknowledge;
        public string bankname;
        public string category1;
        public string category2;
        public string categoryid;
        public string categoryname;
        public bool isschool;
        public string creator;
    }
}
