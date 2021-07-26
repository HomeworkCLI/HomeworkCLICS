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
        private static OssClient ossClient;

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
#if !DEBUG
                Console.WriteLine("We need to collect some basic information to make sure the software run smoothly.");
                Console.WriteLine("Firstly, please enter the model and mac address you want to use.");
                Console.WriteLine("We need this because login will be recorded by iFlyTek and cannot be deleted.");
                Console.Write("Model:");
                string model = Console.ReadLine();
                Console.Write("Mac address:");
                string mac = Console.ReadLine();
                File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "userDeviceSettings.dat"), );
                user.Machine = model;
                user.Mac = mac;
#endif
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
#if DEBUG
            JObject userDeviceSettings = JObject.Parse($"{{model:\"{user.Machine}\",mac:\"{user.Mac}\"}}");
#else
            JObject userDeviceSettings = JObject.Parse(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "userDeviceSettings.dat")));
#endif
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
                    Console.WriteLine($"start get sts token");
                    JObject getOssSecretKeyNewResult = user.getOssSecretKeyNew().Result;
                    JObject ossInfo = JObject.Parse(rc4Decrypt(getOssSecretKeyNewResult.Value<string>("data")));
                    Console.WriteLine($"sts token successfully got");
#if DEBUG
                    Console.WriteLine($"sts token: accessKeyId={ossInfo.Value<string>("accessKeyId")}");
                    Console.WriteLine($"sts token: accessKeySecret={ossInfo.Value<string>("accessKeySecret")}");
                    Console.WriteLine($"sts token: securityToken={ossInfo.Value<string>("securityToken")}");
                    Console.WriteLine($"sts token: expiration={ossInfo.Value<string>("expiration")}");
#endif
                    ossClient = new OssClient("http://oss-cn-hangzhou.aliyuncs.com",
                        ossInfo.Value<string>("accessKeyId"),
                        ossInfo.Value<string>("accessKeySecret"),
                        ossInfo.Value<string>("securityToken"));
                    foreach (string file in dialog.FileNames)
                    {
                        string uuid = Guid.NewGuid().ToString();
                        Console.WriteLine($"start uploading {file}");
                        using (var fs = File.Open(file, FileMode.Open))
                        {
                            var putObjectRequest = new PutObjectRequest("yixuexiao-2", $"aliba/upload/HomeworkUpload/{uuid}/0.0{Path.GetExtension(file)}", fs);
                            putObjectRequest.StreamTransferProgress += streamProgressCallback;
                            ossClient.PutObject(putObjectRequest);
                        }
                        Console.WriteLine();
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
                        Console.WriteLine($"share data: {user.ShareDoc("1", string.Empty, saveDocInfo.Value<JObject>("data").Value<string>("docid"), user.Userid).Result.Value<string>("msg")}");
                    }
                }
                Console.WriteLine("Press any key to upload more files.");
                _ = Console.ReadKey(true);
            }
        }

        private static string rc4Decrypt(string input)
        {
            // #region rc4 decrypt
            int size = input.Length;
            byte[] ret = new byte[(size / 2)];
            byte[] tmp = Encoding.UTF8.GetBytes(input);
            for (int i = 0; i < size / 2; i++)
            {
                char b0 = (char)(((char)Convert.ToByte(Encoding.UTF8.GetString(new byte[] { tmp[i * 2] }), 16)) << 4);
                ret[i] = (byte)(b0 ^ ((char)Convert.ToByte(Encoding.UTF8.GetString(new byte[] { tmp[i * 2 + 1] }), 16)));
            }
            byte a = 0;
            byte[] b = { a };
            byte[] raw = new byte[input.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = Convert.ToByte(input.Substring(i * 2, 2), 16);
            }

            byte[] bkey = Encoding.UTF8.GetBytes("FD4DB9C94A694B87A34DDC563B36010E");
            byte[] state = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                state[i] = (byte)i;
            }
            int index1 = 0;
            int index2 = 0;
            if (bkey == null || bkey.Length == 0)
            {
                state = null;
            }
            for (int i2 = 0; i2 < 256; i2++)
            {
                index2 = ((bkey[index1] & 255) + (state[i2] & 255) + index2) & 255;
                byte tmp2 = state[i2];
                state[i2] = state[index2];
                state[index2] = tmp2;
                index1 = (index1 + 1) % bkey.Length;
            }

            int x = 0;
            int y = 0;
            byte[] key = state;
            byte[] result = new byte[ret.Length];
            for (int i = 0; i < ret.Length; i++)
            {
                x = (x + 1) & 255;
                y = ((key[x] & 255) + y) & 255;
                byte tmp3 = key[x];
                key[x] = key[y];
                key[y] = tmp3;
                result[i] = (byte)(ret[i] ^ key[((key[x] & 255) + (key[y] & 255)) & 255]);
            }
            return Encoding.UTF8.GetString(result);
        }
        private static void streamProgressCallback(object sender, StreamTransferProgressArgs args)
        {
            Console.Write($"\rProgress: {((double)args.TransferredBytes / args.TotalBytes):0.00%}");
        }
    }
}