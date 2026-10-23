using System;
using System.IO;
using System.Windows.Forms;
namespace Upocourt  {
    static class Program  {
        [STAThread]static int Main(string[] args) {
#if UPOCOURT_TESTS
            if(args.Length>0&&args[0]=="--test")return Tests.Run();
            if(args.Length>0&&args[0]=="--ui-test")return Tests.Ui();
#endif
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try {
                using(var form=new GameForm()) {
#if UPOCOURT_TESTS
                    if(args.Length>1&&args[0]=="--screenshot") {
                        form.Show();
                        Application.DoEvents();
                        form.Preview(Path.GetFullPath(args[1]),args.Length>2&&args[2]=="court");
                        return 0;
                    }
#endif
                    Application.Run(form);
                }
                return 0;
            }
            catch(Exception e) {
                string path=Path.Combine(Path.GetTempPath(),"Upocourt-error.txt");
                File.WriteAllText(path,e.ToString());
                MessageBox.Show("Upocourt could not continue. Details: "+path,"Upocourt");
                return 1;
            }
        }
    }
}
