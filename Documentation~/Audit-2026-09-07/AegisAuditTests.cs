using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
namespace Astra.Aegis.Tests {
public sealed class AegisAuditTests {
[Test] public void BuiltInRule_AssetRoundTrip() {
 const string p="Assets/AuditRule.asset";
 var r=ScriptableObject.CreateInstance<MissingMonoScriptRule>();
 try { AssetDatabase.CreateAsset(r,p); AssetDatabase.SaveAssets();
 Resources.UnloadAsset(r); AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceUpdate); Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<MissingMonoScriptRule>(p),"Saved built-in rule cannot be reloaded");
 } finally {AssetDatabase.DeleteAsset(p);}
}
[Test] public void SerializedTraversal_TerminatesOnCycle() {
 var r=ScriptableObject.CreateInstance<AegisAuditFixture>();
 r.node=new AegisAuditNode();r.node.next=r.node;
 try {using(var so=new SerializedObject(r)) {var it=so.GetIterator();int n=0;while(it.NextVisible(true)&&n<1000)n++;
 Assert.Less(n,1000,"NextVisible(true) repeatedly visits cyclic managed references");}}
 finally{UnityEngine.Object.DestroyImmediate(r);}
}
[Test] public void MissingReference_ScansHiddenSerializedField() {
 const string p="Assets/AuditFixture.asset";
 var r=ScriptableObject.CreateInstance<AegisAuditFixture>();
 var rule=ScriptableObject.CreateInstance<MissingObjectReferenceRule>();
 try{AssetDatabase.CreateAsset(r,p);AssetDatabase.SaveAssets();Resources.UnloadAsset(r);
 string yaml=File.ReadAllText(p).Replace("hiddenReference: {fileID: 0}","hiddenReference: {fileID: 11400000, guid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa, type: 2}").Replace("visibleReference: {fileID: 0}","visibleReference: {fileID: 11400000, guid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa, type: 2}");
 File.WriteAllText(p,yaml);AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceUpdate);r=AssetDatabase.LoadAssetAtPath<AegisAuditFixture>(p);
 var f=new List<AegisFinding>();
 typeof(MissingObjectReferenceRule).GetMethod("EvaluateObject",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(rule,new object[]{p,r,new AegisFindingSink(f)});
 Assert.IsTrue(f.Exists(x=>x.PropertyPath=="visibleReference"),"Fixture must have a broken visible reference");
 Assert.IsTrue(f.Exists(x=>x.PropertyPath=="hiddenReference"),"Hidden serialized missing reference is ignored");
 }finally{AssetDatabase.DeleteAsset(p);UnityEngine.Object.DestroyImmediate(rule);}
}
[UnityTest] public IEnumerator CancelledRun_IsNotSuccessful() {
 AegisRunResult completed=null;var run=new AegisInteractiveRun(new AegisValidationProfile(),r=>completed=r);
 run.Start();run.Cancel();while(completed==null)yield return null;
 Assert.IsFalse(completed.Success,"Cancelled incomplete report is reported as successful");
}
[Test] public void BuiltInRule_HasPersistableScript() {
 var r=ScriptableObject.CreateInstance<MissingMonoScriptRule>();
 try { var script=MonoScript.FromScriptableObject(r); Assert.IsNotNull(script,"Built-in rule has no MonoScript"); Assert.AreEqual(typeof(MissingMonoScriptRule),script.GetClass(),"MonoScript cannot resolve rule type"); }
 finally { UnityEngine.Object.DestroyImmediate(r); }
}
[UnityTest] public IEnumerator InteractiveRun_RespectsSuppressions() {
 var profile=new AegisValidationProfile(); profile.IncludedFolders.Add("Assets/DoesNotExist");
 var sync=new AegisRunner().Run(profile).Report;
 // Ensure a deterministic finding even if the validation project has build scenes.
 var scenes=EditorBuildSettings.scenes;
 EditorBuildSettings.scenes=new EditorBuildSettingsScene[0];
 sync=new AegisRunner().Run(profile).Report;
 Assert.Greater(sync.Findings.Count,0);
 string fingerprint=sync.Findings[0].Fingerprint;
 AegisSettings.instance.AddSuppression(fingerprint,"audit","audit");
 AegisRunResult completed=null;
 var run=new AegisInteractiveRun(profile,r=>completed=r);
 try { run.Start(); while(completed==null) yield return null;
 Assert.IsFalse(new List<AegisFinding>(completed.Report.Findings).Exists(f=>f.Fingerprint==fingerprint),"Suppressed finding reappears in interactive report");
 } finally {run.Cancel();AegisSettings.instance.RemoveSuppression(fingerprint);EditorBuildSettings.scenes=scenes;}
}
[Test] public void JUnit_SuppressedFindingsDoNotFail() {
 var records=new List<AegisRuleExecutionRecord>{new AegisRuleExecutionRecord("rule","Rule",AegisRuleExecutionStatus.Findings,1,0,"")};
 var report=new AegisValidationReport("CI",0,new List<AegisFinding>(),records);
 string p=Path.GetTempFileName();
 try { AegisReportWriters.WriteJUnit(report,p); Assert.IsFalse(File.ReadAllText(p).Contains("<failure "),"No active findings but JUnit reports failure"); }finally{File.Delete(p);}
}
[Test] public void JUnit_DisabledRuleIsSkipped() {
 var report=new AegisValidationReport("CI",0,new List<AegisFinding>(),new List<AegisRuleExecutionRecord>{new AegisRuleExecutionRecord("rule","Rule",AegisRuleExecutionStatus.Skipped,0,0,"Disabled")});
 string p=Path.GetTempFileName();try{AegisReportWriters.WriteJUnit(report,p);StringAssert.Contains("<skipped",File.ReadAllText(p));}finally{File.Delete(p);}
}
}}
