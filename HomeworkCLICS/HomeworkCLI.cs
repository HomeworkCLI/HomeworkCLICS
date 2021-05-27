using Aliyun.OSS;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.IO.Compression;

namespace HomeworkCLI
{
    internal class HomeworkCLI
    {
        private static readonly HomeworkCLICore user = new HomeworkCLICore();
        private static readonly OssClient ossClient = new OssClient("http://oss-cn-hangzhou.aliyuncs.com", "LTAI4G8HWjQYmcTk735N1zxu", "WnoFodPmNvhT1wjnh73CiMf3QTaNnB");

        [STAThread]
        private static void Main(string[] args)
        {
            Console.WriteLine($"HomeworkCLI(Version {App.Version})");
            #region Check update
            Console.WriteLine("Checking for update...");
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.GetAsync("http://fs.yixuexiao.cn/aliba/upload/HomeworkUpload/HomeworkCLI/updateInfo.json").ContinueWith(antecendent =>
                {
                    JObject updateInfo = JObject.Parse(antecendent.Result.Content.ReadAsStringAsync().Result);
                    if (updateInfo.Value<int>("versionCode") > App.VersionCode)
                    {
                        DialogResult result = MessageBox.Show($"A newer version is detected, update?\nBelow is update details.\n{updateInfo.Value<string>("updateDetails")}", "Updater", MessageBoxButtons.YesNo);
                        if (result == DialogResult.Yes)
                        {
                            Console.WriteLine("Downloading...");
                            httpClient.GetAsync(updateInfo.Value<string>("downloadUrl")).ContinueWith(antecendent =>
                            {
                                string updateFilePath = Path.Combine(Environment.GetEnvironmentVariable("temp"), "HomeworkCLI.zip");
                                if (File.Exists(updateFilePath)) File.Delete(updateFilePath);
                                FileStream updateFileStream = new FileStream(updateFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                                antecendent.Result.Content.ReadAsStreamAsync().Result.CopyTo(updateFileStream);
                                Console.WriteLine("Download finished.");
                                Console.WriteLine("Checking file integrity...");
                                byte[] hash = SHA256.Create().ComputeHash(updateFileStream);
                                StringBuilder builder = new StringBuilder();
                                for (int i = 0; i < hash.Length; i++)
                                {
                                    builder.Append(hash[i].ToString("X2"));
                                }
                                Console.WriteLine("SHA256: " + builder.ToString());
                                if (builder.ToString() != updateInfo.Value<string>("hashCode"))
                                {
                                    Console.WriteLine("File is broken.\n Press any key to exit...");
                                    _ = Console.ReadKey(true);
                                    Environment.Exit(1);
                                }
                                Console.WriteLine("Unzipping file");
                                string updateFloderPath = Path.Combine(Directory.GetCurrentDirectory(), "updateFiles");
                                if (Directory.Exists(updateFloderPath)) Directory.Delete(updateFloderPath, true);
                                ZipFile.ExtractToDirectory(updateFilePath, updateFloderPath);
                                using Process process = new Process
                                {
                                    StartInfo = new ProcessStartInfo(
                                        Path.Combine(Directory.GetCurrentDirectory(),
                                        "update.bat"))
                                };
                                process.Start();
                                Environment.Exit(0);
                            }).Wait();
                        }
                    }
                    else
                    {
                        Console.WriteLine("up-to-date");
                    }
                }).Wait();
            };
            #endregion Check update
            #region Init
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "userDetailInfo.dat")))
            {
                Console.WriteLine("Welcome to HomeworkCLI");
                Console.WriteLine("By pressing any key you have read and agreed to the open source agreement(Apache License 2.0) of this software.");
                _ = Console.ReadKey(true);
                Console.WriteLine("We need to collect some basic information to make sure the software run smoothly.");
                Console.WriteLine("Firstly, please enter the model and mac address you want to use.");
                Console.WriteLine("We need this because login will be recorded by iFlyTek and cannot be deleted.");
                Console.Write("Model:");
                string model = Console.ReadLine();
                Console.Write("Mac address:");
                string mac = Console.ReadLine();
                File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "userDeviceSettings.dat"), $"{{model:\"{model}\",mac:\"{mac}\"}}");
                user.Machine = model;
                user.Mac = mac;
                Console.WriteLine("Secondly, please login your account");
                Console.Write("username: ");
                string username = Console.ReadLine();
                Console.Write("password: ");
                StringBuilder password = new StringBuilder();
                while (true)
                {
                    var i = Console.ReadKey(true);
                    if (i.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                        break;
                    }
                    else if (i.Key == ConsoleKey.Backspace)
                    {
                        if (password.Length > 0)
                        {
                            password.Remove(password.Length - 1, 1);
                            Console.Write("\b \b");
                        }
                    }
                    else
                    {
                        password.Append(i.KeyChar);
                        Console.Write("*");
                    }
                }
                user.ClientLogin(username, password.ToString(), true).ContinueWith(antecendent =>
                {
                    if (antecendent.Result.Value<int>("code") == 1)
                    {
                        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "userDetailInfo.dat"), antecendent.Result.ToString());
                        Console.WriteLine("Logged in");
                    }
                    else
                    {
                        Console.WriteLine($"Login failed with message: {antecendent.Result.Value<string>("msg")}");
                        Console.WriteLine("Program exiting...");
                        _ = Console.ReadKey(true);
                        Environment.Exit(1);
                    }
                }).Wait();
            }
            #endregion Init
            user.ClientLoginWithJSON(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "userDetailInfo.dat")));
            JObject userDeviceSettings = JObject.Parse(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "userDeviceSettings.dat")));
            user.Machine = userDeviceSettings.Value<string>("model");
            user.Mac = userDeviceSettings.Value<string>("mac");
            Console.WriteLine($"Hi, {user.DisplayName}");
            while (true)
            {
                Console.WriteLine("Please select your files.");
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Multiselect = true,
                    Title = "请选择文件",
                    Filter = "所有文件(*.*)|*.*",
                };
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string file in dialog.FileNames)
                    {
                        string uuid = Guid.NewGuid().ToString();
                        Console.WriteLine($"start uploading {file}");
                        ossClient.PutObject("yixuexiao-2", $"aliba/upload/HomeworkUpload/{uuid}/0.0{Path.GetExtension(file)}", file);
                        Console.WriteLine($"successfully uploaded, uuid: {uuid}");
                        DocInfo docInfo = new DocInfo
                        {
                            title = Path.GetFileName(file),
                            doctype = Path.GetExtension(file).Substring(1),
                            docsize = new FileInfo(file).Length,
                            dir = "aliba/upload/HomeworkUpload",
                            key = "aliba/upload/HomeworkUpload/" + uuid + "/0.0" + Path.GetExtension(file),
                            md5code = uuid,
                            guid = uuid,
                            isconverth5 = false,
                            ispublish = false,
                            agent = "android",
                            iflyknowledge = string.Empty,
                            bankname = "体育",
                            category1 = string.Empty,
                            category2 = string.Empty,
                            categoryid = "ghnvak6jnkdoh0hg1pdowq",
                            categoryname = "课件",
                            isschool = false,
                            creator = user.Userid,
                        };
                        JObject saveDocInfo = user.SaveDocNew(docInfo).Result;
                        Console.WriteLine($"saveDocNew data: {saveDocInfo.Value<string>("msg")}");
                        Console.WriteLine($"shar data: {user.ShareDoc("1", string.Empty, saveDocInfo.Value<JObject>("data").Value<string>("docid"), user.Userid).Result.Value<string>("msg")}");
                    }
                }
                Console.WriteLine("Press any key to upload more files.");
                _ = Console.ReadKey(true);
            }
        }

        private class OssUploadThread
        {
            private readonly string bucket = "yixuexiao-2";
            private readonly string key = string.Empty;
            private readonly string file = string.Empty;

            public OssUploadThread(string key, string file)
            {
                this.key = key;
                this.file = file;
            }

            public void Upload()
            {
                Console.WriteLine("1");
                ossClient.PutObject(this.bucket, this.key, this.file);
            }
        }
    }

}