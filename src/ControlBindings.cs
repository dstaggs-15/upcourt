using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
namespace Upcourt {
public static class ControlBindings {
    public static readonly string[] PadActions={"Shoot / block","Pass / steal","Switch player","Crossover / fake","Screen / box out","Jump","Spin","Step back","Dunk / alley oop","Punch","Call a cut","Playbook","Sprint","Lob modifier"};
    public static readonly string[] PadDefaults={"X","A","B","Y","RB","LB","LT+Y","LT+B","LT+X","LT+RB","LT+A","DPadUp","RT","LB"};
    public static readonly string[] UtilityActions={"Sprint","Timeout","Pause","Mute"};
    public static readonly string[] UtilityDefaults={"ShiftKey","T","Escape","M"};
    public static readonly string[] Buttons={"A","B","X","Y","LB","RB","LT","RT","LStick","RStick","DPadUp","DPadDown","DPadLeft","DPadRight"};
    public static readonly int[] Flags={Input.A,Input.B,Input.X,Input.Y,Input.LB,Input.RB,0x10000,0x20000,0x40,0x80,Input.Up,Input.Down,Input.Left,Input.Right};
    public static int Mask(string binding){if(string.IsNullOrEmpty(binding))return 0;int mask=0;foreach(string part in binding.Split('+')){int index=Array.IndexOf(Buttons,part);if(index<0)return 0;mask|=Flags[index];}return mask;}
    public static bool Matches(string binding,int state){int mask=Mask(binding);return mask!=0&&(state&mask)==mask;}
    public static bool Active(Settings settings,int action,int state){string token=settings.PadBindings[action];if(!Matches(token,state))return false;if(action>=12)return true;int mask=Mask(token);for(int i=0;i<12;i++){int other=Mask(settings.PadBindings[i]);if(i!=action&&other!=mask&&(other&mask)==mask&&(state&other)==other)return false;}return true;}
    public static bool BindPad(Settings settings,int action,string token){if(action<0||action>=PadActions.Length||Mask(token)==0)return false;string previous=settings.PadBindings[action];if(action<13)for(int i=0;i<13;i++)if(i!=action&&settings.PadBindings[i]==token)settings.PadBindings[i]=previous;settings.PadBindings[action]=token;return true;}
    public static string KeyAt(Settings settings,int action){return action<Settings.Actions.Length?settings.Keys[action]:settings.UtilityKeys[action-Settings.Actions.Length];}
    static void SetKey(Settings settings,int action,string key){if(action<Settings.Actions.Length)settings.Keys[action]=key;else settings.UtilityKeys[action-Settings.Actions.Length]=key;}
    public static bool BindKey(Settings settings,int action,Keys key){if(action<0||action>=Settings.Actions.Length+UtilityActions.Length||key==Keys.None)return false;string value=key.ToString(),previous=KeyAt(settings,action);for(int i=0;i<Settings.Actions.Length+UtilityActions.Length;i++)if(i!=action&&KeyAt(settings,i)==value)SetKey(settings,i,previous);SetKey(settings,action,value);return true;}
    public static Keys Utility(Settings settings,int action){Keys key;return Enum.TryParse<Keys>(settings.UtilityKeys[action],out key)?key:Keys.None;}
    public static string CaptureToken(int state,int previous){int fresh=state&~previous;for(int i=0;i<Buttons.Length;i++){if(Flags[i]==0x10000||Flags[i]==0x20000)continue;if((fresh&Flags[i])==0)continue;string prefix=(state&0x10000)!=0?"LT+":(state&0x20000)!=0?"RT+":"";return prefix+Buttons[i];}return null;}
    public static void ResetKeyboard(Settings settings){settings.Keys=new Settings().Keys;settings.UtilityKeys=(string[])UtilityDefaults.Clone();}
    public static void ResetPad(Settings settings){settings.PadBindings=(string[])PadDefaults.Clone();settings.SwapSticks=false;settings.InvertMoveY=false;}
    public static void Ensure(Settings settings){if(settings.PadBindings==null||settings.PadBindings.Length!=PadActions.Length||settings.PadBindings.Any(b=>Mask(b)==0))settings.PadBindings=(string[])PadDefaults.Clone();if(settings.UtilityKeys==null||settings.UtilityKeys.Length!=4)settings.UtilityKeys=(string[])UtilityDefaults.Clone();}
}
}
