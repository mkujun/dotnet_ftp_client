using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace FtpLib
{
    public class FtpFacade
    {
        private string Url { get; set; }
        private string Password { get; set; }
        private string User { get; set; }
        private string FolderPath { get; set; }
        private FtpWebRequest FtpWebRequest { get; set; }

        public FtpFacade(string url, string password, string user, string folderPath)
        {
            Url = url;
            Password = password;
            User = user;
            FolderPath = folderPath;
        }

        public FtpWebRequest CreateRequest(string directoryName, string fileName, string method)
        {
            if (!string.IsNullOrEmpty(directoryName) && string.IsNullOrEmpty(fileName))
            {
                FtpWebRequest = (FtpWebRequest)WebRequest.Create(Url + FolderPath + directoryName);
            }

            else if (!string.IsNullOrEmpty(directoryName) && !string.IsNullOrEmpty(fileName))
            {
                FtpWebRequest = (FtpWebRequest)WebRequest.Create(Url + FolderPath + directoryName + "/" + fileName);
            }
            else
            {
                FtpWebRequest = (FtpWebRequest)WebRequest.Create(Url);
            }

            FtpWebRequest.Credentials = new NetworkCredential(User, Password);
            FtpWebRequest.Method = method;

            return FtpWebRequest;
        }

        public List<string> GetDirectory(string directoryName)
        {
            try
            {
                FtpWebRequest request = CreateRequest(directoryName, "", WebRequestMethods.Ftp.ListDirectory);

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    Console.WriteLine($"{response}");

                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(responseStream))
                        {
                            List<string> listing = new List<string>();

                            while (!reader.EndOfStream)
                            {
                                listing.Add(reader.ReadLine());
                            }

                            return new List<string>(listing);
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void CreateDirectory(string directoryName)
        {
            try
            {
                FtpWebRequest request = CreateRequest(directoryName, "", WebRequestMethods.Ftp.MakeDirectory);
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                Console.WriteLine($"Create Directory Complete, status {response.StatusDescription}");
            }
            catch (WebException e)
            {
                Console.WriteLine($"Directory {directoryName} Exists!");
            }
        }

        public void DeleteDirectory(string directoryName)
        {
            List<string> directories = GetDirectory(directoryName);

            if (directories == null)
            {
                try
                {
                    FtpWebRequest request = CreateRequest(directoryName, "", WebRequestMethods.Ftp.RemoveDirectory);
                    FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                    Stream responseStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(responseStream);
                    Console.WriteLine(reader.ReadToEnd());

                    Console.WriteLine($"Directory {directoryName} Deleted, status {response.StatusDescription}");

                    reader.Close();
                    response.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Directory {directoryName} Does Not Exist!");
                }
            }

            else
            {
                ClearDirectory(directoryName);
                DeleteDirectory(directoryName);
            }
        }

        public void ClearDirectory(string directoryName)
        {
            List<string> directoryFiles = GetDirectory(directoryName);

            if (directoryFiles != null)
            {
                foreach (var file in directoryFiles)
                {
                    DeleteFile(directoryName, file.Split('/')[1]);
                }
            }
        }

        public void DeleteFile(string directoryName, string fileName)
        {
            FtpWebRequest request = CreateRequest(directoryName, fileName, WebRequestMethods.Ftp.DeleteFile);

            string result = string.Empty;
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            long size = response.ContentLength;

            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            result = reader.ReadToEnd();

            Console.WriteLine($"File {fileName} Deleted, status {response.StatusDescription}");

            reader.Close();
            responseStream.Close();
            response.Close();
        }

        public void CreateFile(string directoryName, string localFilePath, string fileName)
        {
            FtpWebRequest request = CreateRequest(directoryName, fileName, WebRequestMethods.Ftp.UploadFile);

            byte[] fileContents;
            using (StreamReader sourceStream = new StreamReader(localFilePath))
            {
                fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
            }
            request.ContentLength = fileContents.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(fileContents, 0, fileContents.Length);
            }

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                Console.WriteLine($"Upload File {fileName} Complete, status {response.StatusDescription}");
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            FtpFacade ftp = new FtpFacade("ftp://ip_address/", "pass", "user", "/");

            Console.ReadLine();
        }
    }
}
