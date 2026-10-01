using System;
using System.Globalization;

namespace RoomStudio {
public readonly struct ArcherFit {
 public readonly double AlphaPerMm,BetaPerMm,Gamma;
 public ArcherFit(double alphaPerMm,double betaPerMm,double gamma){
  CtShieldMath.ValidateCoefficients(alphaPerMm,betaPerMm,gamma);
  AlphaPerMm=alphaPerMm;BetaPerMm=betaPerMm;Gamma=gamma;
 }
}

public static class CtShieldMath {
 public static bool IsFinite(double value){return !double.IsNaN(value)&&!double.IsInfinity(value);}
 public static bool TryNumber(string text,double minimum,double maximum,out double value){
  value=0;string input=(text??"").Trim();
  if(!IsFinite(minimum)||!IsFinite(maximum)||maximum<minimum||input.EndsWith(".",StringComparison.Ordinal)||input.EndsWith("e",StringComparison.OrdinalIgnoreCase)||input.EndsWith("+",StringComparison.Ordinal)||input.EndsWith("-",StringComparison.Ordinal))return false;
  return double.TryParse(input,NumberStyles.Float,CultureInfo.InvariantCulture,out value)&&IsFinite(value)&&value>=minimum&&value<=maximum;
 }
 public static void ValidateCoefficients(double alpha,double beta,double gamma){
  if(!IsFinite(alpha)||alpha<=0||!IsFinite(beta)||!IsFinite(gamma)||gamma<=0||
   !IsFinite(alpha+beta)||alpha+beta<=0||!IsFinite(alpha*gamma)||alpha*gamma<=0)
   throw new ArgumentOutOfRangeException("fit","Invalid Archer coefficients.");
 }
 static void CheckFit(ArcherFit fit){ValidateCoefficients(fit.AlphaPerMm,fit.BetaPerMm,fit.Gamma);}
 static double Log1p(double value){
  if(!IsFinite(value)||value<=-1)throw new ArgumentOutOfRangeException("value","log1p domain.");
  if(Math.Abs(value)>=1e-4)return Math.Log(1+value);
  return value*(1+value*(-.5+value*(1.0/3+value*(-.25+value*.2))));
 }
 static double Expm1(double value){
  if(Math.Abs(value)>=1e-5)return Math.Exp(value)-1;
  return value*(1+value*(.5+value*(1.0/6+value*(1.0/24+value/120))));
 }
 public static double LogTransmission(ArcherFit fit,double thicknessMm){
  CheckFit(fit);
  if(!IsFinite(thicknessMm)||thicknessMm<0)throw new ArgumentOutOfRangeException("thicknessMm");
  if(thicknessMm==0)return 0;
  double ratio=fit.BetaPerMm/fit.AlphaPerMm,exponent=fit.AlphaPerMm*fit.Gamma*thicknessMm;
  if(!IsFinite(ratio)||!IsFinite(exponent))throw new OverflowException("Fit/thickness exceeds numeric range.");
  double logB=-(exponent+Log1p(ratio*(-Expm1(-exponent))))/fit.Gamma;
  if(!IsFinite(logB)||logB>0)throw new ArithmeticException("Invalid computed log transmission.");
  return logB;
 }
 public static double Transmission(ArcherFit fit,double thicknessMm){return Math.Exp(LogTransmission(fit,thicknessMm));}
 public static double ThicknessFromLogTransmission(ArcherFit fit,double logB){
  CheckFit(fit);
  if(!IsFinite(logB)||logB>0)throw new ArgumentOutOfRangeException("logB","Finite logB <= 0 required.");
  if(logB==0)return 0;
  double ratio=fit.BetaPerMm/fit.AlphaPerMm,exponent=-fit.Gamma*logB;
  if(!IsFinite(ratio)||!IsFinite(exponent))throw new OverflowException("Fit/target exceeds numeric range.");
  double numerator=exponent<=50?Log1p(Expm1(exponent)/(1+ratio)):exponent+Log1p(ratio*Math.Exp(-exponent))-Log1p(ratio);
  double thickness=numerator/(fit.AlphaPerMm*fit.Gamma);
  if(!IsFinite(thickness)||thickness<0)throw new ArithmeticException("Invalid computed thickness.");
  return thickness;
 }
 public static double ThicknessFromTransmission(ArcherFit fit,double transmission){
  if(!IsFinite(transmission)||transmission<=0||transmission>1)throw new ArgumentOutOfRangeException("transmission");
  return ThicknessFromLogTransmission(fit,Math.Log(transmission));
 }
 public static double RequiredPathThicknessMm(ArcherFit fit,double unshieldedMgyPerWeek,double occupancy,double goalMgyPerWeek){
  CheckFit(fit);
  if(!IsFinite(unshieldedMgyPerWeek)||unshieldedMgyPerWeek<0||!IsFinite(occupancy)||occupancy<=0||occupancy>1||
   !IsFinite(goalMgyPerWeek)||goalMgyPerWeek<=0)throw new ArgumentOutOfRangeException("designInputs");
  if(unshieldedMgyPerWeek==0)return 0;
  double logRequired=Math.Log(goalMgyPerWeek)-Math.Log(occupancy)-Math.Log(unshieldedMgyPerWeek);
  return logRequired>=0?0:ThicknessFromLogTransmission(fit,logRequired);
 }
 public static double LogAdd(double logFirst,double logSecond){
  if(double.IsNaN(logFirst)||double.IsNaN(logSecond)||double.IsPositiveInfinity(logFirst)||double.IsPositiveInfinity(logSecond))
   throw new ArgumentOutOfRangeException("logContribution");
  if(double.IsNegativeInfinity(logFirst))return logSecond;
  if(double.IsNegativeInfinity(logSecond))return logFirst;
  double larger=Math.Max(logFirst,logSecond),smaller=Math.Min(logFirst,logSecond);
  return larger+Log1p(Math.Exp(smaller-larger));
 }
}
}