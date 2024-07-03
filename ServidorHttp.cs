using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Web;

namespace HttpServerScratch
{
    internal class ServidorHttp
    { 
        private TcpListener Controller { get; set; }

        private int port { get; set; }
        /// <summary>
        /// Monitore the quantitiy of request received
        /// </summary>
        private int QtdRequests { get; set; }

        private string HtmlExample { get; set; }
        // Mime types supported from this server
        private SortedList<string,string> MimeTypes { get; set; }
        private SortedList<string,string> HostsDirectories { get; set; }

        public ServidorHttp(int porta = 8080) 
        { 
            this.port = porta;
            this.QtdRequests = 0;
            CreateHtmlExample();
            PopulateMimeTypes();
            PopulateHostDirectories();

            try
            {
                //Initializing the server
                this.Controller = new TcpListener(IPAddress.Parse("127.0.0.1"), this.port);
                this.Controller.Start();
                Console.WriteLine($"The Server is Running on port: {this.port}");
                Console.WriteLine($"To Access digit on the browser http://localhost:{this.port}.");
                Task HttpServerTask = Task.Run(() => AwaitingRequests()); //do in paralel
                HttpServerTask.GetAwaiter().GetResult(); //to program await the exit for the HttpServerTask

            }
            catch (Exception ex) {
                Console.WriteLine($"[ERROR] Intializing the Server on port {this.port} error message: {ex.Message}");
            }
        
        }

        private async Task AwaitingRequests()
        {
            while ( true )
            {
                Socket Connection = await this.Controller.AcceptSocketAsync();
                this.QtdRequests++;

                //Create a new thread to Proccess the Request paralel
                Task task = Task.Run(() => ProcessRequest(Connection,this.QtdRequests));
            }
        }

        private void ProcessRequest(Socket Connection, int RequestNumber)
        {
            Console.WriteLine($"Processing Request #{RequestNumber}...");
            if (Connection.Connected)
            {
                byte[] bytesRequisicao = new byte[1024];
                //put the bytes from requisition in the bytearray, if not completed puts with 0 to the end
                Connection.Receive(bytesRequisicao, bytesRequisicao.Length, 0);
                //converting the bytes for string
                string RequisitionText = Encoding.UTF8.GetString(bytesRequisicao)
                    .Replace((char)0, ' ').Trim(); //this line removes the 0 from the string

                if (RequisitionText.Length > 0) {

                    Console.WriteLine($"{RequisitionText}");

                    string[] requisitionLines = RequisitionText.Split("\r\n"); //this is the break line
                    // have better ways to do this but using this case only for practicity
                    // GET /address.html http/1.1  <- message to be parsed
                    int iFirstSpace                 = requisitionLines[0].IndexOf(' ');
                    int iSecondSpace                = requisitionLines[0].LastIndexOf(' ');
                    string httpMethodRequested      = requisitionLines[0].Substring(0, iFirstSpace);
                    //get the filename to be requested
                    string resourceRequested        = requisitionLines[0].Substring(iFirstSpace + 1, iSecondSpace - iFirstSpace - 1);
                    if (resourceRequested == "/") resourceRequested = "/index.html";

                    //geting the url parameters
                    string parametersUrl = resourceRequested.Contains("?") ? resourceRequested.Split("?")[1] : "";
                    SortedList<string,string> parameters = ParametersProcessing(parametersUrl);

                    //if the server is send a form in post you get the form with "\r\n\r\n"
                    string postData = RequisitionText.Contains("\r\n\r\n") ? RequisitionText.Split("\r\n\r\n")[1] : "";
                    if (!string.IsNullOrEmpty(postData))
                    {
                        postData = HttpUtility.UrlDecode(postData);
                        //the data send in post form is like the url Sending id=2&name=rice
                        SortedList<string,string> postParameters = ParametersProcessing(postData);
                        foreach (KeyValuePair<string,string> parameter in postParameters) 
                        {
                            //parameters[parameter.Key] = parameter.Value;
                            parameters.Add(parameter.Key, parameter.Value);
                        }
                    }


                    resourceRequested               = resourceRequested.Split("?")[0]; // thi is to handle inputs parameters in URL
                    string httpVersion              = requisitionLines[0].Substring(iSecondSpace + 1);

                    //now getting the host
                    iFirstSpace                     = requisitionLines[1].IndexOf(' ');
                    string hostName                 = requisitionLines[1].Substring(iFirstSpace + 1);

                    byte[] bytesContent             = null;
                    byte[] RequestGenerated         = null;
                    FileInfo fileRequested          = new FileInfo(GetFullPathFile(hostName,resourceRequested));

                    if(fileRequested.Exists){
                        //check if the file requested is supported by the server
                        if (MimeTypes.ContainsKey(fileRequested.Extension.ToLower())) {

                            if(fileRequested.Extension.ToLower() == ".dhtml")
                            {
                                bytesContent = GenerateDynamicHTML(fileRequested.FullName, parameters, httpMethodRequested);

                            }
                            else
                            {
                                bytesContent = File.ReadAllBytes(fileRequested.FullName);
                            }

                            string mimeTypeResponse = MimeTypes[fileRequested.Extension.ToLower()];
                            RequestGenerated = GenerateHeader(httpVersion, mimeTypeResponse, "200", bytesContent.Length);

                        }
                        else
                        {
                            bytesContent = Encoding.UTF8.GetBytes("<h1>ERROR 415 - MIME TYPE NOT SUPPORTED </h1>");
                            RequestGenerated = GenerateHeader(httpVersion, "text/html;charset=utf-8", "415", bytesContent.Length);
                        }
                        
                    }
                    else
                    {
                        bytesContent = Encoding.UTF8.GetBytes("<h1>ERROR 404 - CONTENT NOT FOUND </h1>");
                        RequestGenerated = GenerateHeader(httpVersion, "text/html;charset=utf-8", "404", bytesContent.Length);

                    }

                    int bytesSent = Connection.Send(RequestGenerated, RequestGenerated.Length, 0);
                    bytesSent += Connection.Send(bytesContent, bytesContent.Length, 0);
                    Connection.Close();
                    Console.WriteLine($"{bytesSent} Bytes sent to the requisition number {RequestNumber}");
                }
            }

            Console.WriteLine($"Request #{RequestNumber} Finished\n");
        }

        public byte[] GenerateHeader(string HttpVersion, string MimeType, string HttpCode, int ContentLengthBytes = 0)
        {
            StringBuilder Header = new StringBuilder();
            Header.Append($"{HttpVersion} {HttpCode}{Environment.NewLine}");
            Header.Append($"Server: Simple C# Http Server 1.0{Environment.NewLine}");
            Header.Append($"Content-type: {MimeType}{Environment.NewLine}");
            Header.Append($"Content-Length: {ContentLengthBytes}{Environment.NewLine}{Environment.NewLine}");
            //transfor the reader in bytes
            return Encoding.UTF8.GetBytes(Header.ToString());

        }

        private void CreateHtmlExample()
        {
            StringBuilder html = new StringBuilder();
            html.Append("<!DOCTYPE html><html lang=\"en-us\"><head><meta charset=\"UTF-8\">");
            html.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            html.Append("<title>Static Page</title></head><body>");
            html.Append("<h1>Static Page</h1></body></html>");
            this.HtmlExample = html.ToString();
        }

        private byte[] ReadFile(string resource)
        {
            //this method is no used anymore
            string directorypath = "D:\\VisualStudioProjects\\HttpServerScratch\\www";
            string FilePath = directorypath + resource.Replace("/","\\");
            if (File.Exists(FilePath)) {

                return File.ReadAllBytes(FilePath);

            }
            else return new byte[0];
        }

        private void PopulateMimeTypes()
        {
            this.MimeTypes = new SortedList<string, string>();
            this.MimeTypes.Add(".html", "text/html;charset=utf-8");
            this.MimeTypes.Add(".dhtml", "text/html;charset=utf-8");
            this.MimeTypes.Add(".css", "text/css");
            this.MimeTypes.Add(".js", "text/js");
            this.MimeTypes.Add(".png", "image/png");
            this.MimeTypes.Add(".jpg", "image/jpg");
            this.MimeTypes.Add(".gif", "image/gif");
            this.MimeTypes.Add(".svg", "image/svg+xml");
            this.MimeTypes.Add(".ico", "image/ico");
            this.MimeTypes.Add(".woff", "font/woff");
            this.MimeTypes.Add(".woff2", "font/woff2");

        }

        private void PopulateHostDirectories()
        {
            this.HostsDirectories = new SortedList<string, string>();
            this.HostsDirectories.Add("localhost", "D:\\VisualStudioProjects\\HttpServerScratch\\www\\localhost");
            this.HostsDirectories.Add("maroquio.com", "D:\\VisualStudioProjects\\HttpServerScratch\\www\\maroquio.com");
        }

        private string GetFullPathFile(string host ,string file)
        {
            //get the first part of the host withour port
            string directory = this.HostsDirectories[host.Split(":")[0]];
            string fullPath = directory + file.Replace("/","\\");
            return fullPath;
        }

        public byte[] GenerateDynamicHTML(string filePath, SortedList<string,string> parametersList, string httpMethod)
        {
            FileInfo templateFile = new FileInfo(filePath);
            string classNamePage = "HttpServerScratch." + "Page" + templateFile.Name.Replace(templateFile.Extension, "");
            
            //calling a class with a string in C#
            Type DynamicPageType = Type.GetType(classNamePage,true,true);
            //converting the Ype object to DynamicPage object
            DynamicPage DLpage = Activator.CreateInstance(DynamicPageType) as DynamicPage;
            DLpage.HtmlTemplate = File.ReadAllText(filePath);

            switch (httpMethod.ToLower())
            {
                case "get":
                    return DLpage.Get(parametersList);
                case "post":
                    return DLpage.Post(parametersList);
                default:
                    return new byte[0];
            }
        }

        private SortedList<string,string> ParametersProcessing(string parametersUrl)
        {
            SortedList<string,string> parameters = new SortedList<string,string>();
            //to parse
            //v=_3_tDDpzS30&t=1200
            if (!string.IsNullOrEmpty(parametersUrl))
            {
                string[] keyValuePairs = parametersUrl.Split("&");
                foreach (string pair in keyValuePairs) {
                    parameters.Add(pair.Split("=")[0].ToLower(), pair.Split("=")[1]); 
                }
            } 

            return parameters;
        }

    }
}
