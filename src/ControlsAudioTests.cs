using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Forms;
namespace Upcourt {
#if UPCOURT_TESTS
public static class ControlsAudioTests {
    public static void Run(Action<bool,string> check){
        Settings settings=new Settings();check(ControlBindings.Mask("LT+RB")==0x10200,"controller combo mask");check(ControlBindings.Mask("invalid")==0,"invalid controller token rejected");
        check(ControlBindings.Active(settings,8,0x10000|Input.X)&&!ControlBindings.Active(settings,0,0x10000|Input.X),"combo suppresses base shoot action");
        check(ControlBindings.BindPad(settings,0,"Y")&&settings.PadBindings[0]=="Y"&&settings.PadBindings[3]=="X","conflicting controller actions swap");
        check(!ControlBindings.BindPad(settings,0,"invalid")&&settings.PadBindings[0]=="Y","bad remap does not alter binding");
        ControlBindings.BindKey(settings,15,Keys.G);check(settings.Keys[15]=="G"&&settings.Keys[16]=="J","keyboard conflicts swap across actions");ControlBindings.BindKey(settings,21,Keys.F);check(settings.UtilityKeys[0]=="F"&&settings.Keys[7]=="ShiftKey","sprint can be rebound with conflict swapping");
        check(ControlBindings.CaptureToken(0x10000|Input.B,0x10000)=="LT+B","controller capture records held modifier");
        check(ControlBindings.CaptureToken(Input.RB,0)=="RB","controller capture records shoulder button");
        ControlBindings.ResetPad(settings);check(settings.PadBindings.SequenceEqual(ControlBindings.PadDefaults)&&settings.Keys[15]=="G","controller reset leaves keyboard preferences intact");ControlBindings.ResetKeyboard(settings);check(settings.Keys.SequenceEqual(new Settings().Keys)&&settings.UtilityKeys.SequenceEqual(ControlBindings.UtilityDefaults),"keyboard reset restores defaults");
        settings.PadBindings=null;settings.UtilityKeys=null;ControlBindings.Ensure(settings);check(settings.PadBindings.Length==14&&settings.UtilityKeys.Length==4,"old saves receive controller and utility defaults");
        string[] names={"stadium","cheer","applause","groan"};string[] hashes={"b7c685db5a72195dd1497e29faba1075d48aa621bacf9b45f5f81b2bf2e98f99","3620b8c69da83e882fef2531d453717a9a7efdca415765d5fa5e96ea4edb6730","658928c573b5fd8aa0adabe998e33eee61ce14ed61c5189e83ed9387c5f3db55","3c8df64ab4bdb5a937822e92d436af251f26cd8fd517f87a64ae2f09124e167d"};
        for(int i=0;i<names.Length;i++){using(var stream=typeof(ArcadeSounds).Assembly.GetManifestResourceStream("Upcourt.Audio."+names[i]+".wav"))using(var sha=SHA256.Create()){check(stream!=null,"real recorded crowd embedded");string actual=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();check(actual==hashes[i],"embedded crowd bytes match the sourced Downfield recording");}float[] recording=CrowdRecordings.Read(names[i]);check(recording.Length>ArcadeSounds.Rate&&recording.Any(x=>Math.Abs(x)>.001f),"recording decoder returns real PCM samples");}
        check(ArcadeSounds.Get("jump").Length==0&&ArcadeSounds.Get("call").Length==0&&ArcadeSounds.Get("squeak").Length==0,"routine moves and automatic squeaks no longer produce electronic tones");check(ArcadeSounds.Get("ambience").Length==35*ArcadeSounds.Rate,"ambience uses recorded evolving crowd");check(ArcadeSounds.Get("bounce").Length<ArcadeSounds.Rate/5,"dribble is a short thump rather than a long tone");
        StadiumMixer preview=new StadiumMixer{Stadium=false,EffectsVolume=0,CrowdVolume=1,Volume=1};preview.Play("preview");preview.Stadium=false;short[] audition=new short[8192];preview.Render(audition,4096);check(audition.Any(v=>v!=0)&&preview.VoiceCount==1,"crowd preview plays in Options independently of effects volume");
        SaveData data=new SaveData();ControlBindings.BindPad(data.Settings,9,"RStick");ControlBindings.BindKey(data.Settings,19,Keys.U);Persistence.Save(data);SaveData loaded=Persistence.Load();check(loaded.Settings.PadBindings[9]=="RStick"&&loaded.Settings.Keys[19]=="U","keyboard and controller mappings persist");
    }
    public static int Ui(GameForm form,BindingFlags flags){try{
        Type type=typeof(GameForm);type.GetMethod("Start",flags).Invoke(form,new object[]{false,true,false});type.GetMethod("Open",flags).Invoke(form,new object[]{"pause"});var choices=(System.Collections.Generic.List<Choice>)type.GetField("choices",flags).GetValue(form);choices.First(c=>c.Title.Contains("XBOX DIAGRAM")).Run();if((string)type.GetField("screen",flags).GetValue(form)!="controller")throw new Exception("pause entry did not open diagram");
        type.GetMethod("Open",flags).Invoke(form,new object[]{"padbindings"});type.GetMethod("StartPadCapture",flags).Invoke(form,new object[]{0});Input input=(Input)type.GetField("input",flags).GetValue(form);type.GetMethod("CaptureControls",flags).Invoke(form,null);typeof(Input).GetField("buttons",flags).SetValue(input,Input.Y);typeof(Input).GetField("old",flags).SetValue(input,0);type.GetMethod("CaptureControls",flags).Invoke(form,null);
        SaveData data=(SaveData)type.GetField("data",flags).GetValue(form);if(data.Settings.PadBindings[0]!="Y"||data.Settings.PadBindings[3]!="X")throw new Exception("controller capture failed to swap bindings");
        input.Clear();type.GetField("menuRelease",flags).SetValue(form,false);type.GetMethod("Open",flags).Invoke(form,new object[]{"game"});Match game=(Match)type.GetField("game",flags).GetValue(form);game.Phase="live";input.TestPadButtons=Input.Y;type.GetMethod("Tick",flags).Invoke(form,null);if(!game.Charging)throw new Exception("remapped controller shoot did not reach gameplay");game.Charge=.72f;input.TestPadButtons=0;type.GetMethod("Tick",flags).Invoke(form,null);if(game.Phase!="shot")throw new Exception("remapped controller release did not fire shot");input.TestPadButtons=null;
        type.GetMethod("Open",flags).Invoke(form,new object[]{"controls"});type.GetField("binding",flags).SetValue(form,15);type.GetMethod("KeyPressed",flags).Invoke(form,new object[]{form,new KeyEventArgs(Keys.G)});if(data.Settings.Keys[15]!="G"||data.Settings.Keys[16]!="J")throw new Exception("keyboard capture failed");input.Clear();type.GetField("menuRelease",flags).SetValue(form,false);type.GetMethod("Back",flags).Invoke(form,null);type.GetMethod("Back",flags).Invoke(form,null);if((string)type.GetField("screen",flags).GetValue(form)!="pause")throw new Exception("controls did not return to pause");
        type.GetMethod("Save",flags).Invoke(form,null);SaveData reloaded=Persistence.Load();if(reloaded.Settings.PadBindings[0]!="Y"||reloaded.Settings.Keys[15]!="G")throw new Exception("control preferences did not persist");ControlBindings.ResetPad(data.Settings);ControlBindings.ResetKeyboard(data.Settings);
        Console.WriteLine("PASS: pause diagram -> pad capture -> remapped shoot/release -> keyboard capture -> save -> return to pause");return 0;
    }catch(Exception e){Console.WriteLine("FAIL: controls UI / "+e);return 1;}}
}
#endif
}
