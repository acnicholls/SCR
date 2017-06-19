using System;
using System.IO;
using System.Security.Permissions;
using System.Text;

namespace SCR
{
	/// <summary>
	/// This class will find the current folder name, then recursively remove all vssver.scc. files 
	/// so that Visual Studio will no longer see the project or files as protected by a 
	/// source control program.  
	/// The user must enter a username and password as command line variables.
	/// 
	/// This code was written by Anthony Charles Nicholls in the year 2010 to encompass an idea he had to expediate
	/// copying a solution and moving on with a new idea while still maintaining code integrity and solution compartmentalization.
	/// </summary>
	class SrcContRem
	{
		private static string logFolder = AppDomain.CurrentDomain.BaseDirectory.ToString() + @"\scr_log\";
		private static string topFolder = AppDomain.CurrentDomain.BaseDirectory.ToString();
		private static bool fileChecked = false;
		private static bool project = false;
		private static bool path = false;
		private static bool provider = false;
		private static string logLine = "";

		/// <summary>
		/// This is the starting point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			// find the current directory name
			DirectoryInfo topDir = new DirectoryInfo(topFolder);
			CheckAndRemoveSourceControlFiles(topDir);
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine("Finished, Please press the 'Enter' key to continue...");
			Console.ReadLine();
			
		}

		private static void WriteLog(string message)
		{
			CheckLogDriectoryExists();
			// this function writes a log of important info and is useful for debugging
			StreamWriter logFile = new StreamWriter(logFolder + "scr.log",true);
			logFile.WriteLine(DateTime.Now + "    " + message);
			logFile.Flush();
			logFile.Close();   
		}

		private static void CheckLogDriectoryExists()
		{
			//string path = AppDomain.CurrentDomain.BaseDirectory + @"\log\";
			DirectoryInfo dir = new DirectoryInfo(logFolder);
			if(!dir.Exists)
				dir.Create();
		}
		
		private static bool checkPermissions(DirectoryInfo d)
		{
			Console.Write("Checking Folder : " + d.Name.ToString() + " ... ");
			logLine = "Checking Folder : " + d.Name.ToString() + " ... ";
			bool Perm = new bool();
			FileIOPermission PermValue = new FileIOPermission(FileIOPermissionAccess.PathDiscovery, d.FullName.ToString());
			try
			{
				PermValue.Demand();
				Console.WriteLine("PERMISSION GRANTED");
				logLine += "PERMISSION GRANTED";
				Perm = true;
			}
			catch(System.Exception e)
			{
				Console.WriteLine("PERMISSION DENIED");
				logLine += "PERMISSION DENIED";
				Perm = false;
			}
			WriteLog(logLine);
			return Perm;
		}

		private static void CheckAndRemoveSourceControlFiles(DirectoryInfo d) 
		{ 
			try
			{
				if (checkPermissions(d))
				{
					int f = d.GetFiles().Length;
					if ( f > 0 )
					{
						FileInfo[] fis = d.GetFiles();
						foreach ( FileInfo fi in fis)
						{
							Console.Write("Checking File : " + fi.Name.ToString() + " ... ");
							logLine = "Checking File : " + fi.Name.ToString() + " ... ";
							fileChecked = false;
							CheckSCCFile(fi);
							if(!fileChecked)
								CheckVSPFile(fi);
							if(!fileChecked)
								CheckProjectFile(fi);
							if(!fileChecked)
							{
								Console.WriteLine("SKIPPED");
								logLine += "SKIPPED";
							}
							WriteLog(logLine);
						}
					}
					// Check All Subdirectories.
					int x = d.GetDirectories().Length;
					if ( x > 0)
					{
						DirectoryInfo[] dis = d.GetDirectories();
						foreach (DirectoryInfo di in dis) 
						{
							CheckAndRemoveSourceControlFiles(di);
						}
					}
				}
			}
			catch(System.Exception e)
			{
					WriteLog("Error : " + e.GetBaseException());
			}
		}

		private static void CheckSCCFile(FileInfo fi)
		{
			if(fi.Name == "vssver.scc")
			{
				fi.Delete();
				Console.WriteLine("DELETED");
				logLine += "DELETED";
				fileChecked = true;
			}
			if(fi.Extension == ".vssscc")
			{
				fi.Attributes -= FileAttributes.ReadOnly;
				fi.Delete();
				Console.WriteLine("DELETED");
				logLine += "DELETED";
				fileChecked = true;
			}
		}

		private static void CheckProjectFile(FileInfo fi)
		{
			// this method requires a check for a specific type of project file, maybe a command line variable could be used to distinguish, 
			// for now it is only C# projects that this works with.
			if(fi.Extension == ".csproj")
			{
				fi.Attributes -= FileAttributes.ReadOnly;
				OpenProjectFileAndRemoveSourceControlInfo(fi);
				if(project && path && provider)
				{
					fileChecked = true;
					Console.WriteLine("MODIFIED");
					logLine += "MODIFIED";
				}
				else
                    fileChecked = false;
			}
		}

		private static void CheckVSPFile(FileInfo fi)
		{
			WriteLog("Extension is " + fi.Extension.ToString());
			if(fi.Extension == ".vspscc")
			{
				//WriteLog("Attributes Before : " + fi.Attributes.ToString());
				File.SetAttributes(fi.FullName.ToString(), FileAttributes.Normal);
				//WriteLog("Attributes After : " + fi.Attributes.ToString());
				fi.Delete();
				Console.WriteLine("DELETED");
				logLine += "DELETED";
				fileChecked = true;
			}
		}

		private static void OpenProjectFileAndRemoveSourceControlInfo(FileInfo fi)
		{
			// this section searches the file for the following text and removes the entire line contianing the text part
			StreamReader sr;
			StreamWriter sw;
			int intStart = 0;
			int intEnd = 0;
			int numOfCharsToRemove = 0;
			try
			{
				sr = File.OpenText(fi.FullName.ToString());
				string strText = sr.ReadToEnd();
				// SccProjectName
				intStart = FindStart(strText, "SccProjectName");
				if(intStart > -1)
				{
					intEnd = FindEnd(strText, intStart);
					numOfCharsToRemove = intEnd - intStart;
					WriteLog("removing " + numOfCharsToRemove + " characters from file.");
					// now remove all characters between start and end.
					strText = strText.Remove(intStart, numOfCharsToRemove);
					//WriteLog("New Text of File : " + strText.ToString());
					project = true;
				}
				// SccLocalPath
				intStart = FindStart(strText, "SccLocalPath");
				if(intStart > -1)
				{
					intEnd = FindEnd(strText, intStart);
					numOfCharsToRemove = intEnd - intStart;
					WriteLog("removing " + numOfCharsToRemove + " characters from file.");
					// now remove all characters between start and end.
					strText = strText.Remove(intStart, numOfCharsToRemove);
					//WriteLog("New Text of File : " + strText.ToString());
					path = true;
				}
				// SccProvider
				intStart = FindStart(strText, "SccProvider");
				if(intStart > -1)
				{
					intEnd = FindEnd(strText, intStart);
					numOfCharsToRemove = intEnd - intStart;
					WriteLog("removing " + numOfCharsToRemove + " characters from file.");
					// now remove all characters between start and end.
					strText = strText.Remove(intStart, numOfCharsToRemove);
					//WriteLog("New Text of File : " + strText.ToString());
					provider = true;
				}
				// the file is then saved and closed.
				sr.Close();
				File.SetAttributes(fi.FullName.ToString(), FileAttributes.Normal);
				sw = new StreamWriter(fi.FullName.ToString(), false);
				sw.Write(strText);
				sw.Flush();
				sw.Close();
				File.SetAttributes(fi.FullName.ToString(), FileAttributes.ReadOnly);
			}
			catch (Exception ex)            
			{                
				WriteLog(ex.Message);            
			}            
		}

		public static int FindStart(string strText, string strSearch)
		{
			int intStart = 0;
			intStart = strText.IndexOf(strSearch, 0, strText.Length);
			WriteLog("Found " + strSearch + " at : " + intStart.ToString());
			return intStart;
		}

		public static int FindEnd(string strText, int intStart)
		{
			int intEnd = 0;
			intEnd = strText.IndexOf(Convert.ToChar(13), intStart);
			WriteLog("Found CarriageReturn at : " + intEnd.ToString());
			return intEnd;
		}

	}
}
