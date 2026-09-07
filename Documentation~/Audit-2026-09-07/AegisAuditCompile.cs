using System.IO;
using UnityEditor;
using UnityEditor.Build.Player;
using MisterPxl.Aegis;
public static class AegisAuditCompile {
 public static void Run(){
 AegisSettings.instance.Save();
 File.WriteAllText("Audit~/profile-probe.txt","Build="+AegisSettings.instance.GetProfile("Build").Name+"; CI="+AegisSettings.instance.GetProfile("CI").Name);
 var result=PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings{target=BuildTarget.StandaloneOSX,group=BuildTargetGroup.Standalone,options=ScriptCompilationOptions.None},"Library/AuditPlayerScripts");
 File.WriteAllText("Audit~/player-probe.txt","Assemblies="+(result.assemblies==null?"null":result.assemblies.Count.ToString()));
 }
}