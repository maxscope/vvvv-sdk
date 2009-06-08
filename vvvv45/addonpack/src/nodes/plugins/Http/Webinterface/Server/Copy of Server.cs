using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Web;
using System.Runtime.Remoting.Messaging;


using VVVV.Webinterface;
using VVVV.Webinterface.Utilities;
using VVVV.Webinterface.Data;
using VVVV.Webinterface.HttpServer;



namespace VVVV.Webinterface.HttpServer {


    /// <summary>
    /// Server Class definiton 
    /// Initiates in Server with the Methods GET and Post
    /// Inherite from the Observer
    /// Server listens for all incoming clients on all IP's
    /// </summary>
    /// 
    class Server1 : Observer
    {

        #region filed declaration

       
        private Socket mMainSocket;
        private ArrayList mClientSocketList = new ArrayList();
        public delegate void DelegateReceiveData();


        private SortedList<string, Socket> mIndexSocketList = new SortedList<string, Socket>();
        private SortedList<string, Socket> mDummySocketList = new SortedList<string, Socket>();

        private List<string> mRequestList = new List<string>();
        private int mClientCount = 0;

        IPEndPoint mIpLocal;

        
        private int mPort;
        private int mBacklog;
        private string mName;
        private string mFolderToServ;

        private ConcreteSubject mSubject;
        private Logger mLogger;
        private bool FDisposed = false;
        private WebinterfaceSingelton mWebinterfaceSingelton = WebinterfaceSingelton.getInstance();

        private ItemsToServ mItemsToServ;
        private List<string> mFileList = new List<string>();
        private List<string> mFileNames = new List<string>();


        //Socket Verwaltung
        public Socket workSoket = null;
        public const int BufferSize = 256;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();


        //VVVV generate Files to Serv
        private string mCssFile;
        private string mJsFile;

        public static ManualResetEvent allDone = new ManualResetEvent(false);


        public string VVVVCssFile
        {
            set
            {
                mCssFile = value;
            }
        }

        public string VVVVJsFile
        {
            set
            {
                mJsFile = value;
            }
        }

        #endregion field Declaration






        #region properties

        /// <summary>
        /// Name of the Server
        /// </summary>
        public string Name
        {
            get
            {
                return mName;
            }
        }

        /// <summary>
        /// sets the port of the Server
        /// </summary>
        public int Port 
        { 
            set 
            {
                mIpLocal.Port = value;
                mLogger.log(mLogger.LogType.Info, "set Server to Port: " + value.ToString());
                Debug.WriteLine("set Server to Port: " + value.ToString());
            } 
        }

        public List<string> FileList
        {
            get
            {
                return mFileList;
            }
        }
        /// <summary>
        /// sets the folder to serve
        /// </summary>

        public List<string> FileNames
        {
            get
            {
                return mFileNames;
            }
        }

        #endregion properties







        #region constructor /deconstructor

        /// <summary>
        /// Server constructor
        /// </summary>
        /// <param name="Port">Server Port</param>
        /// <param name="Backlog">Backlog </param>
        /// <param name="pSubject">the subject class which cooperates with the server</param>
        /// <param name="pName">name of the server</param>
        public Server1(int Port, int Backlog, ConcreteSubject pSubject, string pName, string pFolderToServ)
        {
			
            Debug.WriteLine("create server");
            mLogger =  mWebinterfaceSingelton.Logger;
            mLogger.log(mLogger.LogType.Info,"Created Server Class");

            mItemsToServ = new ItemsToServ(pFolderToServ);

            this.mPort = Port;
            this.mBacklog = Backlog;
            this.mName = pName;
            this.mSubject = pSubject;


        }


        #region Dispose

        public void Dispose()
        {
            Dispose(true);
            // Take yourself off the Finalization queue
            // to prevent finalization code for this object
            // from executing a second time
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.



        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!FDisposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                }
                // Release unmanaged resources. If disposing is false,
                // only the following code is executed.
                //mSocket.Close();
                foreach (KeyValuePair<string, Socket> pKey in mDummySocketList)
                {
                    Socket pSocket = pKey.Value;
                    pSocket.Shutdown(SocketShutdown.Both);
                    pSocket.Close();
                }

                
                mMainSocket.Shutdown(SocketShutdown.Both);
                mMainSocket.Close();

                mIndexSocketList.Clear();
                mIndexSocketList = null;
                mDummySocketList.Clear();
                mDummySocketList = null;

                Debug.WriteLine("Server is being deleted");
                mLogger.log(mLogger.LogType.Info, "Server Class is beeing deleted");

                
                // Note that this is not thread safe.
                // Another thread could start disposing the object
                // after the managed resources are disposed,
                // but before the disposed flag is set to true.
                // If thread safety is necessary, it must be
                // implemented by the client.
            }
            FDisposed = true;
        }

        #endregion Dispose


        #endregion constructor /deconstructor






        #region ServeData

        public void ServeFolder(string pPath)
        {
             mFolderToServ = pPath;
             mLogger.log(mLogger.LogType.Info, "serve Folder: " + pPath);
             Debug.WriteLine("serve Folder: " + pPath);
             mItemsToServ.ReadServerFolder(pPath);

             mFileList = mItemsToServ.FileListVVVV;
             mFileNames = mItemsToServ.FileListNameVVVV;
        }

        #endregion ServeData





        public void StartListining()
        {
            try
            {
                mIpLocal = new IPEndPoint(IPAddress.Any, mPort);
                mMainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                mMainSocket.Bind(mIpLocal);
                // Start listening...<
                mMainSocket.Listen(100);
                // Create the call back for any client connections...
                while (true)
                {
                    allDone.Reset();
                    mMainSocket.BeginAccept(new AsyncCallback(OnClientConnectCallback), mMainSocket);
                    allDone.WaitOne();
                }

            }
            catch (SocketException se)
            {
                Debug.WriteLine(se.Message.ToString());
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Server Constructor: \n" + ex.ToString());
            }
        }




        #region request Handle
        /// <summary>
        /// Callback function with is calles if an Client connects to the Server
        /// </summary>
        /// <param name="asynConnect"> Contains the SocketInformationObjekt</param>
        public void OnClientConnectCallback(IAsyncResult asynConnect)
        {
            try
            {
                Debug.WriteLine("-------------onClientConnect----------------" + Environment.NewLine);

                allDone.Set();

                //The Socket witch connects to the Server
                Socket tClientSocket = mMainSocket.EndAccept(asynConnect);

                //Adding the Socket into the Socketlist of the Server 
                mClientSocketList.Add(tClientSocket);
                
                //Adding Time and Socket to the SocketInformation Object 
                SocketInformation tSocketInformations = new SocketInformation(tClientSocket, "Id");
                tSocketInformations.TimeStamp = DateTime.Now;

                //Shows if the Socket is stille connected and begins to receive data in calling the ReceiveSocketDataCallback function
                if (tSocketInformations.ClientSocket.Connected)
                {
                    tClientSocket.BeginReceive(tSocketInformations.Buffer, 0, tSocketInformations.BufferSize,0, new AsyncCallback(ReceiveSocketDataCallback), tSocketInformations);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message.ToString());
            }
        }


        /// <summary>
        /// callback function with receives Data from the Socket 
        /// </summary>
        /// <param name="asynReceive">Contains the SocketInformationObjekt</param>
        public void ReceiveSocketDataCallback(IAsyncResult asynReceive)
        {
            try
            {
                Debug.WriteLine("-------------ReceiveSocketData----------------" + Environment.NewLine);
                
                //gets the SocketInformatinObject form the IAsyncResult Object
                SocketInformation tSocketInformation = (SocketInformation)asynReceive.AsyncState;

                //Looks how many bytes are to read from the socket
                int bytesRead = tSocketInformation.ClientSocket.EndReceive(asynReceive);

                //Looks if all Data is received from the Socket
                if (bytesRead > 0)
                {
                    //Adding the received data to the SocketInformation Object 
                    tSocketInformation.Request.Append(Encoding.ASCII.GetString(tSocketInformation.Buffer, 0, bytesRead));

                    String Content = String.Empty;
                    Content = tSocketInformation.Request.ToString();

                    //looks if alle data is received from the Socket
                    // ?? sind damit alle F�lle abgedeckt (debuggen);
                    if (Content.IndexOf("") > -1)
                    {
                        Request tRequest = new Request(tSocketInformation.Request.ToString());
                        if(tRequest.RequestType == "GET")
                        {
                            SendData(tSocketInformation);
                        }
                        else if (tRequest.RequestType == "POST")
                        {
                            SendData(tSocketInformation);
                        }
                        else
                        {
                            SendData(tSocketInformation);
                        }
                        
                    }
                    else
                    {
                        //if not all Data is received read next block
                       tSocketInformation.ClientSocket.BeginReceive(tSocketInformation.Buffer, 0, tSocketInformation.BufferSize, 0, 
                       new AsyncCallback(ReceiveSocketDataCallback), tSocketInformation);
                    }
                }
             }
    
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        


        private void SendData(SocketInformation pSocketInformations)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(pSocketInformations.Request.ToString());
            pSocketInformations.ClientSocket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendDataCallback), pSocketInformations);

        }


        private void SendDataCallback(IAsyncResult asynSendData)
        {

            try
            {
                SocketInformation tSocketIformations = (SocketInformation)asynSendData.AsyncState;

                int bytesSent = tSocketIformations.ClientSocket.EndSend(asynSendData);
                Console.WriteLine( "Sent {0} bytes to client.",  bytesSent );

                tSocketIformations.ClientSocket.Shutdown( SocketShutdown.Both );
                tSocketIformations.ClientSocket.Close();
           }
            catch ( Exception e )
            {
                Debug.WriteLine( e.ToString() );
            }
        }

        




        #region Outdated

        /// <summary>
        /// async class which is call by an client request
        /// </summary>
        /// <param name="asyn">status of the async operation</param>
        public void OnClientConnectOut(IAsyncResult asyn)
        {
            try
            {

                Debug.WriteLine("-------------onClientConnect----------------" + Environment.NewLine);

                Socket tClientSocket = mMainSocket.EndAccept(asyn);
                mClientSocketList.Add(tClientSocket);

                string tIpAdresse = ((IPEndPoint)tClientSocket.RemoteEndPoint).Address.ToString();
                string tPort = ((IPEndPoint)tClientSocket.RemoteEndPoint).Port.ToString();
                string tSocketIp = tIpAdresse + tPort;
                Debug.WriteLine("SocketId: " + tSocketIp);

                SocketInformation tSocketInformation = new SocketInformation(tClientSocket, tSocketIp);

                tClientSocket.BeginReceive(tSocketInformation.Buffer,0,tSocketInformation.Buffer.Length,SocketFlags.None,new AsyncCallback(ReceiveSocketDataCallback),tSocketInformation);

                
                
                //Thread.Sleep(100);

                int intAvailableThreads, intAvailableIoAsynThreds;
                ThreadPool.GetAvailableThreads(out intAvailableThreads, out intAvailableIoAsynThreds);
                string strMessage = String.Format(@"In OnClientConnect Is Thread Pool: {0}, Thread Id: {1}, Free Threads {2}", Thread.CurrentThread.IsThreadPoolThread.ToString(), Thread.CurrentThread.GetHashCode(), intAvailableThreads.ToString());
                Debug.WriteLine(strMessage);


                

                Debug.WriteLine("TRemoteEndPoint: " + ((IPEndPoint)tClientSocket.RemoteEndPoint).ToString() + " / IP Adressse: " + tIpAdresse.ToString() + " / Port: " + tPort);

                String str = String.Format("Client # {0} connected", mClientCount);
                mLogger.log(mLogger.LogType.Info, str + "with IP: " + tIpAdresse + " on Port: " + tPort);
                Debug.WriteLine(str);


                string tRequest = getContent(tClientSocket);
                mRequestList.Add(tRequest);


                mLogger.log(mLogger.LogType.Info, tRequest);


                Debug.WriteLine("");
                Debug.WriteLine("-----------------------------");
                Debug.WriteLine(tRequest);
                Debug.WriteLine("-----------------------------");
                Debug.WriteLine("");

                String[] lines = tRequest.Split(Environment.NewLine.ToCharArray());
                String[] words = lines[0].Split(" ".ToCharArray());




                if (words[0] == "GET")
                {

                    int pos = words[1].LastIndexOf("/");
                    String filename = words[1].Substring(pos + 1);

                    if (filename == "index.html")
                    {

                        if (mIndexSocketList.ContainsKey(tIpAdresse) == true)
                        {
                            int tKey = mIndexSocketList.IndexOfKey(tIpAdresse);
                            Socket tSocket;
                            mIndexSocketList.TryGetValue(tIpAdresse, out tSocket);
                            //tSocket.Shutdown(SocketShutdown.Both);

                            mIndexSocketList.Remove(tIpAdresse);
                            mDummySocketList.Remove(tIpAdresse);

                            --mClientCount;
                            mIndexSocketList.Add(tIpAdresse, tClientSocket);

                        }
                        else
                        {
                            mIndexSocketList.Add(tIpAdresse, tClientSocket);
                        }

                        //Nachschauen ob es seite schon gibt
                        SortedList<string, string> mServerDaten = mWebinterfaceSingelton.ServerDaten;
                        if (mServerDaten.ContainsKey(filename))
                        {
                            string tHtmlPage;
                            mServerDaten.TryGetValue(filename, out tHtmlPage);

                            ResponseHeader tHeader = new ResponseHeader(new HTTPStatusCode("").Code200, filename);
                            tHeader.SetAttribute("connection", "keep-alive");
                            tHeader.SetAttribute("Content-Length", tHtmlPage.Length.ToString());
                            tHeader.SetAttribute("Content-Type", "text/html");
                            ResponseText tResponse = new ResponseText(tHeader.HeaderText, tClientSocket, filename, tHtmlPage);

                            Thread tGetHandlingThread = new Thread(new ThreadStart(tResponse.Run));
                            tGetHandlingThread.Start();
                        }
                    }
                    else if (filename == "dummy.html")
                    {

                        if (mDummySocketList.ContainsKey(tIpAdresse) == false)
                        {
                            mDummySocketList.Add(tIpAdresse, tClientSocket);
                        }

                        ResponseHeader tHeader = new ResponseHeader(new HTTPStatusCode("").Code200);
                        tHeader.SetAttribute("Cache-Control", "no-cache,must-revalidate");
                        tHeader.SetAttribute("Content-Type", "text/html");

                        Page tPage = new Page(false);
                        string tWholePage = "<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">";


                        ResponseTextDummy tResponse = new ResponseTextDummy(tHeader.HeaderText + Environment.NewLine, tClientSocket, filename, tWholePage);

                        Thread tGetHandlingThread = new Thread(new ThreadStart(tResponse.Run));
                        tGetHandlingThread.Start();
                        Debug.WriteLine("dummy.html");

                    }
                    else
                    {

                        if (filename.Contains(".jpg") || filename.Contains(".png"))
                        {


                            byte[] tPic = mItemsToServ.GetPicContent(filename);
                            int tPiclength = tPic.Length;
                            ResponseHeader tHeader = new ResponseHeader(new HTTPStatusCode("").Code200, filename);

                            tHeader.SetAttribute("Accept-Ranges", "bytes");
                            tHeader.SetAttribute("Content-length", tPiclength.ToString());
                            //tHeader.SetAttribute("Content-Language", "en-US");

                            //tHeader.SetAttribute("Cache-Control", "no-store, no-cache, must-revalidate, post-check=0, pre-check=0");
                            tHeader.SetAttribute("Connection", "keep-alive");

                            if (filename.Contains(".jpg"))
                            {
                                tHeader.SetAttribute("Content-Type", "image/jpg");
                            }
                            else
                            {
                                tHeader.SetAttribute("Content-Type", "image/png");
                            }

                            //tHeader.SetAttribute("Pragma", "no-cache");
                            Debug.WriteLine("Response Header: ");
                            Debug.WriteLine(tHeader.HeaderText);


                            ResponsePic tResponse = new ResponsePic(tHeader.HeaderText, tClientSocket, filename, tPic);

                            Thread tGetHandlingThread = new Thread(new ThreadStart(tResponse.Run));
                            tGetHandlingThread.Start();
                        }
                        else if (filename.Contains(".js"))
                        {

                            string tContent = mItemsToServ.GetFileContent(filename);

                            ResponseHeader tHeader = new ResponseHeader(new HTTPStatusCode("").Code200, filename);
                            tHeader.SetAttribute("Content-Type", " application/x-javascript");
                            tHeader.SetAttribute("Connection", "keep-alive");
                            ResponseText tResText;
                            if (filename == "VVVV.js")
                            {
                                tHeader.SetAttribute("Content-length", mJsFile.Length.ToString());
                                tResText = new ResponseText(tHeader.HeaderText, tClientSocket, filename, mJsFile);
                            }
                            else
                            {
                                tHeader.SetAttribute("Content-length", tContent.Length.ToString());
                                tResText = new ResponseText(tHeader.HeaderText, tClientSocket, filename, tContent);
                            }




                            Thread tGetHandlingThread = new Thread(new ThreadStart(tResText.Run));
                            tGetHandlingThread.Start();
                        }

                        else if (filename.Contains(".css"))
                        {

                            string tContent = mItemsToServ.GetFileContent(filename);
                            ResponseHeader tHeader = new ResponseHeader(new HTTPStatusCode("").Code200, filename);
                            tHeader.SetAttribute("Content-Type", "text/css");

                            ResponseText tResText;
                            if (filename == "VVVV.css")
                            {
                                tHeader.SetAttribute("Content-length", mCssFile.Length.ToString());
                                tResText = new ResponseText(tHeader.HeaderText, tClientSocket, filename, mCssFile);

                            }
                            else
                            {
                                tHeader.SetAttribute("Content-length", tContent.Length.ToString());
                                tResText = new ResponseText(tHeader.HeaderText, tClientSocket, filename, tContent);
                            }

                            Thread tGetHandlingThread = new Thread(new ThreadStart(tResText.Run));
                            tGetHandlingThread.Start();
                        }
                        else
                        {
                            //ResponseHeader tHeader = new ResponseHeader(new HTTPStatusCode("").Code200, filename);
                            //tHeader.SetAttribute("Content-Type", "image/jpeg");

                            //Thread tGetHandlingThread = new Thread(new ThreadStart(tResponse.Run));
                            //tGetHandlingThread.Start();

                            //Page tErrorPage = new Page(true);
                            //tErrorPage.Head.Insert("<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">");
                            //tErrorPage.Body.Insert("404 NOT FOUND");
                            Debug.WriteLine("unknown Filename");
                            mLogger.log(mLogger.LogType.Error, "Unnkown Filename by GET Request");
                        }
                    }
                }
                else if (words[0] == "POST")
                {
                    Debug.WriteLine("POST Request");
                    int tAnzahlLinien = lines.Length;
                    int pos = words[1].LastIndexOf("/");
                    String filename = words[1].Substring(pos + 1);

                    int firstLineIndex = 0;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Contains("?"))
                        {
                            firstLineIndex = i;
                        }
                    }

                    string pContent = "";
                    for (int i = firstLineIndex; i < lines.Length; i++)
                    {
                        pContent += lines[i] + Environment.NewLine;
                    }


                    if (firstLineIndex != 0)
                    {
                        mWebinterfaceSingelton.setNewBrowserDaten(pContent);

                    }

                    ResponseHeader tHeader = new ResponseHeader(new HTTPStatusCode("").Code200, filename);
                    ResponseText tResText = new ResponseText(tHeader.HeaderText, tClientSocket, filename, "");

                    Thread tGetHandlingThread = new Thread(new ThreadStart(tResText.Run));
                    tGetHandlingThread.Start();

                }
                else
                {
                    Debug.WriteLine("Unknown Request");
                    mLogger.log(mLogger.LogType.Error, "Unnkown Request Type");
                }

                ++mClientCount;

                // Until the main Socket is now free, it can go back and wait for
                // other clients who are attempting to connect
                mMainSocket.BeginAccept(new AsyncCallback(OnClientConnectCallback), null);
            }
            catch (ObjectDisposedException)
            {
                
            }
            catch (SocketException se)
            {
                
            }
        }


        /// <summary>
        /// reads bytes from the socket
        /// </summary>
        /// <param name="pSocket"></param>
        /// <returns>returns content string</returns>
        /// 
        private String getContent(Socket pSocket)
        {
            String content = "";
            while (pSocket.Available > 0)
            {
                int bytes = pSocket.Available;
                byte[] buffer = new byte[bytes];
                pSocket.Receive(buffer, bytes, SocketFlags.None);
                content += Encoding.UTF8.GetString(buffer);
                Thread.Sleep(10);
            }

            return content;
        }

        


        private void CloseSocket(IAsyncResult asyn)
        {
            int intAvailableThreads, intAvailableIoAsynThreds;
            ThreadPool.GetAvailableThreads(out intAvailableThreads, out intAvailableIoAsynThreds);
            string strMessage = String.Format(@"In CloseSocket Is Thread Pool: {0}, Thread Id: {1}, Free Threads {2}", Thread.CurrentThread.IsThreadPoolThread.ToString(), Thread.CurrentThread.GetHashCode(), intAvailableThreads.ToString());
            Debug.WriteLine(strMessage);
        }

        #endregion Outdated






        #region Updated Handling
        /// <summary>
        /// sends the new data to the browser
        /// </summary>
        public override void Updated()
        {
            Debug.WriteLine("Updated Server");
            mLogger.log(mLogger.LogType.Info, "Server updated because of Value Change in VVVV");
            SortedList<string, string> tNewServerhandlingDaten = mSubject.NewServerDaten;

            foreach (KeyValuePair<string, string> tSlice in tNewServerhandlingDaten)
            {

                if (tSlice.Key.Contains("Checkbox"))
                {
                    Debug.WriteLine("Checkbox Update");
                    JavaScript tJava = new JavaScript();
                    tJava.Insert("parent.window.setCheckbox('" + tSlice.Key + "','" + tSlice.Value + "');");
                    JavaScript tJava2 = new JavaScript();
                    tJava2.Insert("parent.window.changeValue('" + tSlice.Key + "');");

                    foreach (KeyValuePair<string, Socket> pKey in mDummySocketList)
                    {

                        Socket pSocket = pKey.Value;
                        ResponseHeader tHeader = new ResponseHeader(new HTTPStatusCode("").Code200, "dummy.html");
                        ResponseUpdate tResponse = new ResponseUpdate(tHeader.HeaderText, pSocket, "dummy.html", tJava.Text + Environment.NewLine + tJava2.Text);
                        
                   }

                }else if(tSlice.Key.Contains("Button"))
                {

                }
                else
                {
                    Debug.WriteLine(" send: " + tSlice.Value + " from VVVV to Browser Element: " + tSlice.Key);
                    mLogger.log(mLogger.LogType.Info, " send: " + tSlice.Value + " from VVVV to Server / Target Element: " + tSlice.Key);
                    JavaScript tJava = new JavaScript();
                    tJava.Insert("parent.window.setNewDaten('" + tSlice.Key + "','" + tSlice.Value + "');");

                    foreach (KeyValuePair<string, Socket> pKey in mDummySocketList)
                    {

                        Socket pSocket = pKey.Value;
                        if (pSocket.Connected)
                        {
                            pSocket.Send(Encoding.UTF8.GetBytes(tJava.Text + Environment.NewLine));
                        }
                        
                        //ResponseHeader tHeader = new ResponseHeader();
                        //ResponseUpdate tResponse = new ResponseUpdate(tHeader.HeaderText, pSocket, "dummy.html", tJava.Text);

                    }


                }
            }
        }

        public override void UpdatedBrowser(string pData)
        {
            foreach (KeyValuePair<string, Socket> pKey in mDummySocketList)
            {
                Socket pSocket = pKey.Value;
                ResponseHeader tHeader = new ResponseHeader(new HTTPStatusCode("").Code200, "dummy.html");

                ResponseTextDummy tResponse = new ResponseTextDummy("", pSocket, "dummy.html", pData);

                Thread tGetHandlingThread = new Thread(new ThreadStart(tResponse.Run));
                tGetHandlingThread.Start();
            }
        }

        /// <summary>
        /// send data to browser to reload the whole HTML page
        /// </summary>
        public override void Reload()
        {

                //foreach (KeyValuePair<string, Socket> pKey in mDummySocketList)
                //{
                //    Socket pSocket = pKey.Value;
                //    pSocket.Close();
                //}

                JavaScript tJava = new JavaScript();
                tJava.Insert("parent.window.Reload();");


                foreach (KeyValuePair<string, Socket> pKey in mDummySocketList)
                {

                    Socket pSocket = pKey.Value;
                    ResponseHeader tHeader = new ResponseHeader(new HTTPStatusCode("").Code200, "dummy.html");

                    ResponseTextDummy tResponse = new ResponseTextDummy("", pSocket, "dummy.html", tJava.Text);

                    Thread tGetHandlingThread = new Thread(new ThreadStart(tResponse.Run));
                    tGetHandlingThread.Start();
                    //pSocket.Send(Encoding.UTF8.GetBytes(pText + Environment.NewLine));
                }
                //sendText(tJava.Text);
                Debug.WriteLine("Reload Server");

            }

         #endregion Updated Handling
            


        #endregion request Handle
    }
}