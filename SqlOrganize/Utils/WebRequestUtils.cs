using System.Net;

namespace SqlOrganize.WebRequestUtils
{
    public static class Utils
    {


        public static void CreateDirectoryIfNotExists(string user, string pass, string upload, string dir)
        {
            WebRequest requestDir = WebRequest.Create(upload + dir);
            requestDir.Method = WebRequestMethods.Ftp.MakeDirectory;
            requestDir.Credentials = new NetworkCredential(user, pass);
            try
            {
                requestDir.GetResponse();
            }
            catch (Exception ex)
            {
                //ignorar excepcion directorio existente
            }
        }

        public static void UploadFile(string user, string pass, string uploadDir, string fileName)
        {
            WebClient client = new WebClient();
            client.Credentials = new NetworkCredential(user, pass);
            client.UploadFile(uploadDir, fileName);

        }
    }
}
