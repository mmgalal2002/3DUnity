using System;
using UnityEngine;

namespace RoomStudio {
public static class CtShieldMathChecks {
 static int assertions;
 static void Require(bool condition,string message){assertions++;if(!condition)throw new Exception("CT math: "+message);}
 static void Near(double actual,double expected,string message,double relative=1e-10,double absolute=1e-12){
  Require(CtShieldMath.IsFinite(actual)&&Math.Abs(actual-expected)<=Math.Max(absolute,Math.Abs(expected)*relative),message+": "+actual+" != "+expected);
 }
 static void Reject(Action operation,string message){bool rejected=false;try{operation();}catch(ArgumentException){rejected=true;}Require(rejected,message);}
 public static void Run(){
  assertions=0;
  var fits=new[]{new ArcherFit(2.246,5.73,.547),new ArcherFit(2.009,3.99,.342),new ArcherFit(.0383,.0142,.658),new ArcherFit(.0336,.0122,.519)};
  double[][] paths={new double[]{0,.5,1,2,3},new double[]{0,.5,1,2,3},new double[]{0,50,100,200,300},new double[]{0,50,100,200,300}};
  double[][] expected={
   new double[]{1,.07885850724852890,.01606369426375395,.001239990179488295,.0001207782315903332},
   new double[]{1,.09659280317034600,.01801306647431474,.001258504786528333,.0001275469390150275},
   new double[]{1,.1030057512057174,.01390034709918690,.0002926267010734886,.000006337276497184778},
   new double[]{1,.1288248487488016,.02096420076642798,.0006748087984019304,.00002313669196541927}
  };
  for(int fitIndex=0;fitIndex<fits.Length;fitIndex++){
   var fit=fits[fitIndex];double previous=2;
   for(int sampleIndex=0;sampleIndex<paths[fitIndex].Length;sampleIndex++){
    double thickness=paths[fitIndex][sampleIndex],transmission=CtShieldMath.Transmission(fit,thickness);
    Near(transmission,expected[fitIndex][sampleIndex],"forward benchmark");
    Require(transmission>0&&transmission<=1&&transmission<previous,"monotonic ordinary transmission");previous=transmission;
    Near(CtShieldMath.ThicknessFromTransmission(fit,transmission),thickness,"inverse roundtrip",1e-10,1e-10);
   }
   foreach(double logTarget in new double[]{-1e-12,-.01,-1,-10,-100,-1000})
    Near(CtShieldMath.LogTransmission(fit,CtShieldMath.ThicknessFromLogTransmission(fit,logTarget)),logTarget,"log inverse roundtrip",1e-10,1e-10);
   double deep=1000/fit.AlphaPerMm,hvl=Math.Log(2)/fit.AlphaPerMm;
   Near(CtShieldMath.LogTransmission(fit,deep+hvl)-CtShieldMath.LogTransmission(fit,deep),-Math.Log(2),"asymptotic HVL");
   Near(CtShieldMath.LogTransmission(fit,1e-8)/1e-8,-(fit.AlphaPerMm+fit.BetaPerMm),"initial slope",1e-6);
   Require(CtShieldMath.IsFinite(CtShieldMath.LogTransmission(fit,100000)),"large thickness retains logB");
  }
  var wood=new ArcherFit(.006020,-.001752,1.483);
  Near(CtShieldMath.ThicknessFromTransmission(wood,CtShieldMath.Transmission(wood,250)),250,"negative-beta wood");
  double unshielded=200*.2/9,physical=unshielded*CtShieldMath.Transmission(fits[0],2);
  Near(physical,.005511067464392423,"reference physical kerma");Near(physical*.25,.001377766866098106,"reference occupied kerma");
  Near(CtShieldMath.RequiredPathThicknessMm(fits[0],unshielded,.25,.002),1.845319208277487,"total required path");
  Near(CtShieldMath.ThicknessFromTransmission(fits[0],.01),1.170970658514653,"one percent lead");
  Near(CtShieldMath.ThicknessFromTransmission(fits[2],.01),108.4300860602584,"one percent concrete");
  Near(CtShieldMath.LogAdd(-1000,-1000),-1000+Math.Log(2),"log sum preserves tiny fields");
  Near(CtShieldMath.LogAdd(double.NegativeInfinity,-10),-10,"true zero contribution");
  Require(CtShieldMath.Transmission(fits[0],100000)==0,"ordinary display underflows without losing logB");
  Require(Math.Abs(CtShieldMath.Transmission(fits[0],2)-Math.Pow(CtShieldMath.Transmission(fits[0],1),2))>.0009,"homogeneous layers are not restarted fits");
  Reject(()=>new ArcherFit(0,1,1),"zero alpha");Reject(()=>new ArcherFit(1,-1,1),"invalid alpha+beta");
  Reject(()=>CtShieldMath.LogTransmission(default(ArcherFit),1),"default struct");
  foreach(double invalid in new double[]{double.NaN,double.PositiveInfinity,-1})Reject(()=>CtShieldMath.LogTransmission(fits[0],invalid),"invalid thickness");
  foreach(double invalid in new double[]{0,-1,1.1,double.NaN,double.PositiveInfinity})Reject(()=>CtShieldMath.ThicknessFromTransmission(fits[0],invalid),"invalid inverse target");
  Reject(()=>CtShieldMath.RequiredPathThicknessMm(fits[0],1,0,.01),"zero occupancy is workflow status");
  Reject(()=>CtShieldMath.RequiredPathThicknessMm(fits[0],1,1,0),"zero goal is workflow status");
  Reject(()=>CtShieldMath.LogAdd(double.NaN,0),"missing data is not log zero");
  Debug.Log("ROOM_STUDIO_CT_MATH_CHECKS_PASSED: "+assertions+" assertions; 20 benchmarks, inverses, units, domains and log-range checks");
 }
}
}