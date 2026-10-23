using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace Upcourt  {
    // PixelPlayers is compiled solely to reuse the exact original pixel font source.
    public class Player  {
        public int Primary=-1,Secondary=-1,Uniform,Team,Number;
        public float VX,VZ,Step,PoseTime;
        public string Pose="idle";
        public bool HasBall;
    }
    public class Installer:Form  {
        string status="PUT UPCOURT ON YOUR DESKTOP";
        bool done;
        public Installer() {
            Text="Install Upcourt";
            ClientSize=new Size(640,400);
            StartPosition=FormStartPosition.CenterScreen;
            DoubleBuffered=true;
            BackColor=Color.FromArgb(24,27,46);
            MouseClick+=(s,e)=> {
                if(e.Y>=210&&e.Y<280&&!done) {
                    try {
                        Install(null);
                        status="INSTALLED / UPCOURT IS ON YOUR DESKTOP";
                        done=true;
                    }
                    catch(Exception error) {
                        status="INSTALL FAILED / "+error.Message;
                    }
                    Invalidate();
                }
                else if(done&&e.Y>=210&&e.Y<280)Close();
            }
            ;
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g=e.Graphics;
            g.FillRectangle(Brushes.Goldenrod,0,0,640,4);
            PixelFont.Draw(g,"UPCOURT",30,45,32,Color.FromArgb(246,235,203));
            PixelFont.Draw(g,"PIXEL BASKETBALL",32,112,12,Color.FromArgb(117,220,162));
            using(var brush=new SolidBrush(Color.FromArgb(60,87,90)))g.FillRectangle(brush,30,210,580,68);
            PixelFont.Draw(g,done?"DONE":"INSTALL GAME",52,231,16,Color.FromArgb(247,195,85));
            PixelFont.Draw(g,status.Length>48?status.Substring(0,48):status,30,320,10,Color.FromArgb(246,235,203));
            PixelFont.Draw(g,"NO ADMIN / LOCAL SAVES / NO EXTRA DOWNLOADS",30,370,10,Color.FromArgb(155,170,162));
        }
        public static string Install(string directory) {
            bool custom=!string.IsNullOrEmpty(directory);
            string path=custom?Path.GetFullPath(directory):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","Upcourt");
            Directory.CreateDirectory(path);
            string exe=Path.Combine(path,"Upcourt.exe"),temp=exe+".tmp";
            using(Stream resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("Upcourt.Payload"))using(var output=File.Create(temp)) {
                if(resource==null)throw new InvalidDataException("Embedded game is missing.");
                resource.CopyTo(output);
            }
            if(File.Exists(exe)) {
                File.Copy(exe,exe+".previous",true);
                File.Copy(temp,exe,true);
                File.Delete(temp);
            }
            else File.Move(temp,exe);
            File.WriteAllText(Path.Combine(path,"Read Me.txt"),"Upcourt - Pixel Basketball\r\nWASD move, Shift sprint, Space hold/release shoot, E pass, Q switch, F crossover, R screen, P playbook, Escape pause. Xbox: left stick move, RT sprint, X shoot, A pass, B switch, Y crossover, RB screen.\r\nSaves: %LOCALAPPDATA%\\Upcourt\\career.xml\r\nTo uninstall, delete this program folder and desktop shortcut. Saves are kept separately.\r\nSource and full guide: https://github.com/dstaggs-15/upcourt\r\n");
            if(!custom) {
                string desktop=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                Type shellType=Type.GetTypeFromProgID("WScript.Shell");
                object shell=Activator.CreateInstance(shellType),shortcut=null;
                try {
                    shortcut=shellType.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[] {
                        Path.Combine(desktop,"Upcourt.lnk")
                    }
                    );
                    shortcut.GetType().InvokeMember("TargetPath",BindingFlags.SetProperty,null,shortcut,new object[] {
                        exe
                    }
                    );
                    shortcut.GetType().InvokeMember("WorkingDirectory",BindingFlags.SetProperty,null,shortcut,new object[] {
                        path
                    }
                    );
                    shortcut.GetType().InvokeMember("Save",BindingFlags.InvokeMethod,null,shortcut,null);
                }
                finally {
                    if(shortcut!=null)Marshal.ReleaseComObject(shortcut);
                    Marshal.ReleaseComObject(shell);
                }
            }
            return exe;
        }
        [STAThread]static int Main(string[] args) {
            if(args.Length==2&&args[0]=="--install-dir") {
                try {
                    Install(args[1]);
                    return 0;
                }
                catch(Exception error) {
                    Console.Error.WriteLine(error);
                    return 1;
                }
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Installer());
            return 0;
        }
    }
}
