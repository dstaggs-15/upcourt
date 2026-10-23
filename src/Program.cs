using System;
using System.IO;
using System.Windows.Forms;
namespace Upcourt  {
    static class Program  {
        [STAThread]static int Main(string[] args) {
#if UPCOURT_TESTS
            if(args.Length>0&&args[0]=="--test")return Tests.Run();
            if(args.Length>0&&args[0]=="--ui-test")return Tests.Ui();
#endif
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try {
                using(var form=new GameForm()) {
#if UPCOURT_TESTS
                    if(args.Length>1&&args[0]=="--screenshot") {
                        form.Show();
                        Application.DoEvents();
                        form.Preview(Path.GetFullPath(args[1]),args.Length>2&&(args[2]=="court"||args[2]=="attack"||args[2]=="dunk"||args[2]=="controller"),args.Length>2?args[2]:"");
                        return 0;
                    }
#endif
                    Application.Run(form);
                }
                return 0;
            }
            catch(Exception e) {
                string path=Path.Combine(Path.GetTempPath(),"Upcourt-error.txt");
                File.WriteAllText(path,e.ToString());
                MessageBox.Show("Upcourt could not continue. Details: "+path,"Upcourt");
                return 1;
            }
        }
    }
}
