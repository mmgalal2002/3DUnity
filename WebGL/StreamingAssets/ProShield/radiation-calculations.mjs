             
            
                          
                          
              
       
       
                  
           
               
                     
          
                 
                  
                      
                           

import {
  PRIMARY_TRANSMISSION_PARAMS,
  SECONDARY_TRANSMISSION_PARAMS,
  DOSE_LIMITS,
  getKvpKey,
  getScatterCategory,
  createDefaultSourceComponents,
} from "./radiation-types.mjs";

const MIN_DISTANCE_METERS = 0.3;
const DEFAULT_WALL_SAMPLE_DENSITY_METERS = 0.1;
const DEFAULT_PLANE_SAMPLE_DENSITY_METERS = 0.5;
const MAX_THICKNESS_MM = 1000;
const LINAC_LEAKAGE_FRACTION = 0.001;
const LINAC_ISOCENTER_DISTANCE_METERS = 1;
const MAZE_CAPTURE_GAMMA_CONSTANT = 4.3e-16;
const MAZE_CAPTURE_GAMMA_TVD_METERS = 6.2;
const MAZE_NEUTRON_WU_CONSTANT = 2.4e-15;
const MAZE_NEUTRON_TVD_FACTOR = 2.06;
const MAZE_DOOR_NEUTRON_TVL_MM = 45;
const MAZE_DOOR_CAPTURE_GAMMA_LEAD_TVL_MM = 61;

                          
               
               
                   
                                       
                                      
                                  
                              
                       
              
                               
 

                         
                   
               
               
 

                                
                       
                                         
 

                                                      
                                    
                   
                         
                         
                     
                       
                               
                         
                                   
 

                                                      
                         
                     
                          
                                                    
                                                    
 

                                                       
                            
                                
                             
                                              
                        
                                      
                     
                               
                              
                         
                
     
 

                                                 
                   
                         
                                     
                                     
                                        
                                            
                                  
                                             
                                  
                                                 
 

                                           
                                             
                    
                       
                               
                                       
                               
                                      
                                           
                                       
                                     
                                   
                                   
                                   
                                      
                                   
                                                  
                                  
                                      
                                      
                                      
 

                                            
                                    
                                               
                                       
                                           
                                    
 

                                             
                          
                                       
                             
                      
                            
                              
                            
                                
                                
                                           
 

                                              
                                  
                                            
                                           
 

                                            
                          
                                   
                                              
                                          
                                  
                                  
                       
                             
 

                                             
                               
                                             
                                      
 

                                              
                     
                                
                                     
                        
                                 
 

                                               
                          
                         
                   
 

                                                 
                         
                     
                          
                                  
                                 
                     
                     
                           
                           
                           
     
 

                                                  
                                  
                                       
                               
                                    
                                
                                                 
                        
                     
                              
                                   
                               
     
 

                            
             
               
                     
                          
                                
 

                                 
                      
                        
                   
                     
                                  
                              
                             
                               
                            
                                    
                              
                             
                                            
                              
                            
                           
 

const LINAC_PRIMARY_TVL_DATA                                               = {
  concrete: [
    { energyMV: 4, tvl1: 350, tvle: 300 },
    { energyMV: 6, tvl1: 370, tvle: 330 },
    { energyMV: 10, tvl1: 410, tvle: 370 },
    { energyMV: 15, tvl1: 440, tvle: 410 },
    { energyMV: 18, tvl1: 450, tvle: 430 },
    { energyMV: 20, tvl1: 460, tvle: 440 },
    { energyMV: 25, tvl1: 490, tvle: 460 },
  ],
  lead: [
    { energyMV: 4, tvl1: 52, tvle: 47 },
    { energyMV: 6, tvl1: 57, tvle: 52 },
    { energyMV: 10, tvl1: 61, tvle: 56 },
    { energyMV: 15, tvl1: 66, tvle: 61 },
    { energyMV: 25, tvl1: 71, tvle: 66 },
  ],
};

const LINAC_LEAKAGE_TVL_DATA                                      = {
  concrete: [
    { energyMV: 4, tvl1: 330, tvle: 280 },
    { energyMV: 6, tvl1: 340, tvle: 290 },
    { energyMV: 10, tvl1: 350, tvle: 310 },
    { energyMV: 15, tvl1: 360, tvle: 330 },
    { energyMV: 18, tvl1: 360, tvle: 340 },
    { energyMV: 20, tvl1: 360, tvle: 340 },
    { energyMV: 25, tvl1: 370, tvle: 350 },
  ],
};

const LINAC_FALLBACK_SECONDARY_TVL_DATA                                  = {
  lead: [
    { energyMV: 4, tvl1: 46, tvle: 41 },
    { energyMV: 6, tvl1: 51, tvle: 47 },
    { energyMV: 10, tvl1: 55, tvle: 51 },
    { energyMV: 15, tvl1: 60, tvle: 55 },
    { energyMV: 25, tvl1: 63, tvle: 58 },
  ],
};

const LINAC_SCATTER_FRACTION_TABLE                         = [
  {
    angleDegrees: 10,
    valuesByEnergy: { 6: 10.4e-3, 10: 16.6e-3, 18: 14.2e-3, 24: 17.8e-3 },
  },
  {
    angleDegrees: 20,
    valuesByEnergy: { 6: 6.73e-3, 10: 5.79e-3, 18: 5.39e-3, 24: 6.32e-3 },
  },
  {
    angleDegrees: 30,
    valuesByEnergy: { 6: 2.77e-3, 10: 3.18e-3, 18: 2.53e-3, 24: 2.74e-3 },
  },
  {
    angleDegrees: 45,
    valuesByEnergy: { 6: 1.39e-3, 10: 1.35e-3, 18: 0.864e-3, 24: 0.83e-3 },
  },
  {
    angleDegrees: 60,
    valuesByEnergy: { 6: 0.824e-3, 10: 0.746e-3, 18: 0.424e-3, 24: 0.386e-3 },
  },
  {
    angleDegrees: 90,
    valuesByEnergy: { 6: 0.426e-3, 10: 0.381e-3, 18: 0.189e-3, 24: 0.174e-3 },
  },
  {
    angleDegrees: 135,
    valuesByEnergy: { 6: 0.3e-3, 10: 0.302e-3, 18: 0.124e-3, 24: 0.12e-3 },
  },
  {
    angleDegrees: 150,
    valuesByEnergy: { 6: 0.287e-3, 10: 0.274e-3, 18: 0.12e-3, 24: 0.113e-3 },
  },
];

const LINAC_CONCRETE_SCATTER_TVL_TABLE                         = [
  {
    angleDegrees: 15,
    valuesByEnergy: { 4: 300, 6: 340, 10: 390, 15: 420, 18: 440 },
  },
  {
    angleDegrees: 30,
    valuesByEnergy: { 4: 250, 6: 260, 10: 280, 15: 310, 18: 320 },
  },
  {
    angleDegrees: 45,
    valuesByEnergy: { 4: 220, 6: 230, 10: 250, 15: 260, 18: 270 },
  },
  {
    angleDegrees: 60,
    valuesByEnergy: { 4: 210, 6: 210, 10: 220, 15: 230, 18: 230 },
  },
  {
    angleDegrees: 90,
    valuesByEnergy: { 4: 170, 6: 170, 10: 180, 15: 180, 18: 190 },
  },
  {
    angleDegrees: 135,
    valuesByEnergy: { 4: 140, 6: 150, 10: 150, 15: 150, 18: 150 },
  },
];

                               
            
            
                   
                              
                              
                             
                               
                            
                                    
                              
                             
                           
                            
                               
                              
                               
                                 
 

                              
             
               
                                  
 

function clamp(value        , min        , max        )         {
  return Math.min(max, Math.max(min, value));
}

export function calculateDistance(
  x1        ,
  y1        ,
  x2        ,
  y2        ,
)         {
  return Math.sqrt(Math.pow(x2 - x1, 2) + Math.pow(y2 - y1, 2));
}

export function calculate3DDistance(
  x1        ,
  y1        ,
  z1        ,
  x2        ,
  y2        ,
  z2        ,
)         {
  return Math.sqrt(
    Math.pow(x2 - x1, 2) + Math.pow(y2 - y1, 2) + Math.pow(z2 - z1, 2),
  );
}

function direction(
  ax        ,
  ay        ,
  bx        ,
  by        ,
  cx        ,
  cy        ,
)         {
  return (cx - ax) * (by - ay) - (cy - ay) * (bx - ax);
}

function crossProduct(
  ax        ,
  ay        ,
  bx        ,
  by        ,
  cx        ,
  cy        ,
)         {
  return (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);
}

function pointInPolygon(point         , polygon           )          {
  let inside = false;

  for (let i = 0, j = polygon.length - 1; i < polygon.length; j = i++) {
    const xi = polygon[i].x;
    const yi = polygon[i].y;
    const xj = polygon[j].x;
    const yj = polygon[j].y;
    const intersects =
      yi > point.y !== yj > point.y &&
      point.x < ((xj - xi) * (point.y - yi)) / (yj - yi || 1e-9) + xi;

    if (intersects) {
      inside = !inside;
    }
  }

  return inside;
}

export function lineIntersectsWall(
  px1        ,
  py1        ,
  px2        ,
  py2        ,
  wall      ,
)          {
  const { x1: wx1, y1: wy1, x2: wx2, y2: wy2 } = wall;

  const d1 = direction(wx1, wy1, wx2, wy2, px1, py1);
  const d2 = direction(wx1, wy1, wx2, wy2, px2, py2);
  const d3 = direction(px1, py1, px2, py2, wx1, wy1);
  const d4 = direction(px1, py1, px2, py2, wx2, wy2);

  return (
    ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
    ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0))
  );
}

export function getIntersectingWalls(
  equipment           ,
  workstation             ,
  walls        ,
)         {
  return walls.filter((wall) =>
    lineIntersectsWall(
      equipment.x,
      equipment.y,
      workstation.x,
      workstation.y,
      wall,
    ),
  );
}

export function calculateTransmissionFactor(
  thickness        ,
  params                    ,
  angleOfIncidence         = 0,
)         {
  const { alpha, beta, gamma } = params;
  const cosTheta = Math.max(Math.cos(angleOfIncidence), 0.1);

  if (thickness <= 0) {
    return 1;
  }

  if (beta === 0) {
    return Math.exp((-alpha * thickness) / cosTheta);
  }

  const betaOverAlpha = beta / alpha;
  const exponent = (alpha * gamma * thickness) / cosTheta;
  const bracket = (1 + betaOverAlpha) * Math.exp(exponent) - betaOverAlpha;

  return clamp(Math.pow(bracket, -1 / gamma), 1e-12, 1);
}

export function calculateRequiredThickness(
  requiredTransmission        ,
  params                    ,
  angleOfIncidence         = 0,
)         {
  const { alpha, beta, gamma } = params;
  const cosTheta = Math.max(Math.cos(angleOfIncidence), 0.1);

  if (requiredTransmission >= 1) return 0;
  if (requiredTransmission <= 0) return MAX_THICKNESS_MM;

  if (beta === 0) {
    return (-Math.log(requiredTransmission) / alpha) * cosTheta;
  }

  const betaOverAlpha = beta / alpha;
  const numerator = Math.pow(requiredTransmission, -gamma) + betaOverAlpha;
  const denominator = 1 + betaOverAlpha;

  return (Math.log(numerator / denominator) / (alpha * gamma)) * cosTheta;
}

function getEquipmentSourceComponents(equipment           )                    {
  if (equipment.sourceComponents?.length) {
    return equipment.sourceComponents;
  }

  return createDefaultSourceComponents(
    equipment.type,
    equipment.x,
    equipment.y,
    equipment.z,
    equipment.workloadDistribution,
    equipment.useFactor,
    equipment.planRotationDegrees,
    equipment.gantryAngleDegrees,
  );
}

function isLinacEquipment(equipment           )          {
  return equipment.type === "linac";
}

function getCalculationStandardForEquipment(equipment           )         {
  return isLinacEquipment(equipment) ? "NCRP 151 / IPEM 75" : "NCRP 147";
}

function getLinacEnergyMV(equipment           )         {
  const rawValue = equipment.kVp >= 1000 ? equipment.kVp / 1000 : equipment.kVp;
  return clamp(rawValue, 4, 25);
}

function interpolateLinear(
  value        ,
  x1        ,
  y1        ,
  x2        ,
  y2        ,
)         {
  if (Math.abs(x2 - x1) < 1e-9) {
    return y1;
  }

  const ratio = (value - x1) / (x2 - x1);
  return y1 + ratio * (y2 - y1);
}

function interpolateFromEntries(
  value        ,
  entries                                 ,
)         {
  const sorted = [...entries].sort((left, right) => left.x - right.x);

  if (value <= sorted[0].x) {
    return sorted[0].y;
  }

  const last = sorted[sorted.length - 1];
  if (value >= last.x) {
    return last.y;
  }

  for (let index = 0; index < sorted.length - 1; index++) {
    const left = sorted[index];
    const right = sorted[index + 1];

    if (value >= left.x && value <= right.x) {
      return interpolateLinear(value, left.x, left.y, right.x, right.y);
    }
  }

  return last.y;
}

function interpolateScatterTableValue(
  angleDegrees        ,
  energyMV        ,
  rows                        ,
)         {
  return interpolateFromEntries(
    angleDegrees,
    rows.map((row) => ({
      x: row.angleDegrees,
      y: interpolateFromEntries(
        energyMV,
        Object.entries(row.valuesByEnergy).map(([energy, value]) => ({
          x: Number(energy),
          y: value,
        })),
      ),
    })),
  );
}

function getLinacFieldSizeCm(equipment           )         {
  return clamp(equipment.linacFieldSizeCm ?? 40, 5, 40);
}

function getLinacScatterAngleDegrees(equipment           )         {
  return clamp(equipment.linacScatterAngleDegrees ?? 90, 30, 150);
}

function getLinacScatterFraction(
  energyMV        ,
  scatterAngleDegrees        ,
)         {
  return interpolateScatterTableValue(
    scatterAngleDegrees,
    energyMV,
    LINAC_SCATTER_FRACTION_TABLE,
  );
}

function getLinacScatterMetadata(equipment           )   
                             
                               
                            
                                    
                              
  {
  if (!isLinacEquipment(equipment)) {
    return {};
  }

  return {
    linacBeamEnergyMV: getLinacEnergyMV(equipment),
    linacWeeklyWorkload: equipment.workload,
    linacFieldSizeCm: getLinacFieldSizeCm(equipment),
    linacScatterAngleDegrees: getLinacScatterAngleDegrees(equipment),
    linacLeakageFactor: clamp(equipment.linacLeakageFactor ?? 1, 1, 10),
  };
}

function getLinacScatterFieldFactor(equipment           )         {
  return Math.pow(getLinacFieldSizeCm(equipment), 2) / 400;
}

function getLinacScatterUseFactor(
  equipment           ,
  component                 ,
)         {
  return clamp(
    Math.max(component.relativeOutput, equipment.useFactor, 0.01),
    0.01,
    1,
  );
}

function getLinacLeakageFraction(component                 )         {
  return clamp(component.relativeOutput || LINAC_LEAKAGE_FRACTION, 1e-4, 0.01);
}

function getLinacLeakageFactor(equipment           )         {
  return clamp(equipment.linacLeakageFactor ?? 1, 1, 10);
}

function getLinacMaterialFamily(material              )                      {
  return material === "lead" || material === "steel" || material === "glass"
    ? "lead"
    : "concrete";
}

function interpolateLinacTvlEntry(
  energyMV        ,
  entries                 ,
)                {
  const sorted = [...entries].sort(
    (left, right) => left.energyMV - right.energyMV,
  );

  if (energyMV <= sorted[0].energyMV) {
    return sorted[0];
  }

  const last = sorted[sorted.length - 1];
  if (energyMV >= last.energyMV) {
    return last;
  }

  for (let index = 0; index < sorted.length - 1; index++) {
    const left = sorted[index];
    const right = sorted[index + 1];

    if (energyMV >= left.energyMV && energyMV <= right.energyMV) {
      return {
        energyMV,
        tvl1: interpolateLinear(
          energyMV,
          left.energyMV,
          left.tvl1,
          right.energyMV,
          right.tvl1,
        ),
        tvle: interpolateLinear(
          energyMV,
          left.energyMV,
          left.tvle,
          right.energyMV,
          right.tvle,
        ),
      };
    }
  }

  return last;
}

function getLinacTvlParams(
  material              ,
  energyMV        ,
  sourceType                     ,
  scatterAngleDegrees         ,
)                 {
  const materialFamily = getLinacMaterialFamily(material);
  const barrierType = sourceType === "primary" ? "primary" : "secondary";

  if (sourceType === "scatter" && materialFamily === "concrete") {
    const meanTvl = interpolateScatterTableValue(
      scatterAngleDegrees ?? 90,
      energyMV,
      LINAC_CONCRETE_SCATTER_TVL_TABLE,
    );

    return {
      tvl1: meanTvl,
      tvle: meanTvl,
      energyMV,
      barrierType,
      materialFamily,
      sourceType,
      model: "mean",
      referenceTvl: meanTvl,
      hvl: meanTvl * 0.301,
      scatterAngleDegrees,
    };
  }

  const entry =
    sourceType === "primary"
      ? interpolateLinacTvlEntry(
          energyMV,
          LINAC_PRIMARY_TVL_DATA[materialFamily],
        )
      : materialFamily === "concrete"
        ? interpolateLinacTvlEntry(energyMV, LINAC_LEAKAGE_TVL_DATA.concrete)
        : interpolateLinacTvlEntry(
            energyMV,
            LINAC_FALLBACK_SECONDARY_TVL_DATA.lead,
          );

  return {
    tvl1: entry.tvl1,
    tvle: entry.tvle,
    energyMV,
    barrierType,
    materialFamily,
    sourceType,
    model: "piecewise",
    referenceTvl: entry.tvl1,
    hvl: entry.tvl1 * 0.301,
    scatterAngleDegrees,
  };
}

function calculateLinacTransmissionFactor(
  thickness        ,
  params                ,
  angleOfIncidence         = 0,
)         {
  if (thickness <= 0) {
    return 1;
  }

  const cosTheta = Math.max(Math.cos(angleOfIncidence), 0.1);
  const effectiveThickness = thickness / cosTheta;

  if (params.model === "mean") {
    return clamp(
      Math.pow(10, -effectiveThickness / params.referenceTvl),
      1e-12,
      1,
    );
  }

  if (effectiveThickness <= params.tvl1) {
    return clamp(Math.pow(10, -effectiveThickness / params.tvl1), 1e-12, 1);
  }

  return clamp(
    0.1 * Math.pow(10, -(effectiveThickness - params.tvl1) / params.tvle),
    1e-12,
    1,
  );
}

function calculateLinacRequiredThickness(
  requiredTransmission        ,
  params                ,
  angleOfIncidence         = 0,
)         {
  const cosTheta = Math.max(Math.cos(angleOfIncidence), 0.1);

  if (requiredTransmission >= 1) return 0;
  if (requiredTransmission <= 0) return MAX_THICKNESS_MM;

  const decades = Math.log10(1 / requiredTransmission);
  if (params.model === "mean") {
    return params.referenceTvl * decades * cosTheta;
  }

  const effectiveThickness =
    decades <= 1
      ? params.tvl1 * decades
      : params.tvl1 + params.tvle * (decades - 1);

  return effectiveThickness * cosTheta;
}

export function calculateLinacSecondaryBarrierBenchmark(
  input                                     ,
)                                       {
  const occupancyAdjustedGoal =
    input.designGoal / Math.max(input.occupancyFactor, 0.001);
  const contributions                          = input.components.map(
    (component, index) => {
      const distanceMeters = Math.max(
        component.distanceMeters,
        MIN_DISTANCE_METERS,
      );
      const scatterAngleDegrees = clamp(
        component.scatterAngleDegrees ?? 90,
        10,
        150,
      );
      const fieldSizeCm = clamp(component.fieldSizeCm ?? 40, 5, 40);
      const useFactor = clamp(component.useFactor ?? 1, 0.01, 1);
      const leakageFactor = clamp(component.leakageFactor ?? 1, 1, 10);
      const workloadAtOneMeter = component.weeklyWorkload * 10;
      const unshieldedAirKerma =
        component.sourceType === "scatter"
          ? (workloadAtOneMeter *
              useFactor *
              getLinacScatterFraction(component.energyMV, scatterAngleDegrees) *
              ((fieldSizeCm * fieldSizeCm) / 400)) /
            Math.pow(distanceMeters, 2)
          : (workloadAtOneMeter * LINAC_LEAKAGE_FRACTION * leakageFactor) /
            Math.pow(distanceMeters, 2);

      return {
        equipmentId: `benchmark-eq-${index}`,
        equipmentName: `Benchmark ${component.energyMV} MV`,
        sourceId: `benchmark-src-${index}`,
        sourceName:
          component.sourceType === "scatter"
            ? `Scatter ${component.energyMV} MV`
            : `Leakage ${component.energyMV} MV`,
        sourceType: component.sourceType,
        calculationStandard: "NCRP 151 / IPEM 75",
        linacBeamEnergyMV: component.energyMV,
        linacWeeklyWorkload: component.weeklyWorkload,
        linacFieldSizeCm:
          component.sourceType === "scatter" ? fieldSizeCm : undefined,
        linacScatterAngleDegrees:
          component.sourceType === "scatter" ? scatterAngleDegrees : undefined,
        linacLeakageFactor:
          component.sourceType === "leakage" ? leakageFactor : undefined,
        unshieldedAirKerma,
        transmissionModel: "linac-tvl",
        linacTvl: getLinacTvlParams(
          input.material,
          component.energyMV,
          component.sourceType,
          scatterAngleDegrees,
        ),
        angleOfIncidence:
          ((component.angleOfIncidenceDegrees ?? 0) * Math.PI) / 180,
      };
    },
  );
  const requiredThickness = solveLinacSecondaryThickness(
    contributions,
    occupancyAdjustedGoal,
    input.combinationRule ?? "auto",
  );

  return {
    requiredThickness,
    occupancyAdjustedGoal,
    unshieldedAirKerma: contributions.reduce(
      (sum, contribution) => sum + contribution.unshieldedAirKerma,
      0,
    ),
    shieldedAirKermaAtRequiredThickness: calculateShieldedAirKermaForThickness(
      contributions,
      requiredThickness,
    ),
    contributions: contributions.map((contribution) => {
      const requiredTransmission = clamp(
        occupancyAdjustedGoal /
          Math.max(contribution.unshieldedAirKerma, 1e-12),
        1e-12,
        1,
      );

      return {
        sourceType:
          contribution.sourceType === "scatter" ? "scatter" : "leakage",
        energyMV: contribution.linacBeamEnergyMV ?? 0,
        unshieldedAirKerma: contribution.unshieldedAirKerma,
        requiredThickness: calculateLinacRequiredThickness(
          requiredTransmission,
          contribution.linacTvl ,
          contribution.angleOfIncidence,
        ),
        referenceTvl: contribution.linacTvl .referenceTvl,
        hvl: contribution.linacTvl .hvl,
      };
    }),
  };
}

export function calculateLinacMazeDoorPhotonDose(
  input                          ,
)                            {
  const useFactor = clamp(input.useFactor, 0.01, 1);
  const headLeakageFraction = clamp(
    input.headLeakageFraction ?? LINAC_LEAKAGE_FRACTION,
    1e-4,
    0.01,
  );
  const fieldFactor = Math.pow(clamp(input.fieldSizeCm ?? 40, 5, 40), 2) / 400;
  const roomScatterCorrectionFactor = Math.max(
    input.roomScatterCorrectionFactor ?? 2.64,
    1,
  );
  const primaryWallFraction = clamp(input.primaryWallFraction ?? 0.34, 0, 1);

  const wallScatterDoseEquivalent =
    input.energies.reduce(
      (sum, energy) =>
        sum +
        (energy.wallScatterWeeklyWorkload ?? energy.weeklyWorkload) *
          Math.max(energy.wallScatterCoefficient ?? 0, 0),
      0,
    ) *
    useFactor *
    input.wallScatterProjectionAreaM2 *
    input.wallScatterReflectionCoefficient *
    input.mazeEntranceProjectionAreaM2 *
    Math.pow(
      input.wallScatterSourceDistanceM *
        input.wallScatterMazeDistanceM *
        input.wallScatterDoorDistanceM,
      -2,
    );

  const headLeakageWallScatterDoseEquivalent =
    input.energies.reduce(
      (sum, energy) =>
        sum +
        (energy.headLeakageWeeklyWorkload ?? energy.weeklyWorkload) *
          Math.max(energy.headLeakageWallScatterCoefficient ?? 0, 0),
      0,
    ) *
    useFactor *
    headLeakageFraction *
    input.headLeakageScatterAreaM2 *
    Math.pow(
      input.headLeakageScatterDistanceM * input.headLeakageDoorDistanceM,
      -2,
    );

  const patientScatterDoseEquivalent =
    input.energies.reduce(
      (sum, energy) =>
        sum +
        (energy.patientScatterWeeklyWorkload ?? energy.weeklyWorkload) *
          Math.max(energy.patientScatterFraction ?? 0, 0),
      0,
    ) *
    useFactor *
    fieldFactor *
    input.patientScatterWallReflectionCoefficient *
    input.headLeakageScatterAreaM2 *
    Math.pow(
      input.patientScatterDistanceM *
        input.patientScatterMazeDistanceM *
        input.patientScatterDoorDistanceM,
      -2,
    );

  const transmittedLeakageDoseEquivalent = input.energies.reduce(
    (sum, energy) =>
      sum +
      (energy.transmittedLeakageWeeklyWorkload ?? energy.weeklyWorkload) *
        useFactor *
        headLeakageFraction *
        Math.max(energy.transmittedLeakageBarrierTransmission ?? 0, 0) *
        Math.pow(input.transmittedLeakageDistanceM, -2),
    0,
  );

  return {
    wallScatterDoseEquivalent,
    headLeakageWallScatterDoseEquivalent,
    patientScatterDoseEquivalent,
    transmittedLeakageDoseEquivalent,
    totalPhotonDoseEquivalent:
      roomScatterCorrectionFactor *
      (primaryWallFraction * wallScatterDoseEquivalent +
        headLeakageWallScatterDoseEquivalent +
        patientScatterDoseEquivalent +
        transmittedLeakageDoseEquivalent),
  };
}

export function calculateLinacMazeCaptureGammaDose(
  input                            ,
)                              {
  const headMaterialFactor = Math.max(input.headMaterialFactor ?? 1, 0);
  const roomReturnFactor = Math.max(input.roomReturnFactor ?? 5.4, 0);
  const apertureReturnFactor = Math.max(input.apertureReturnFactor ?? 1.3, 0);
  const captureGammaConstant = Math.max(
    input.captureGammaConstant ?? MAZE_CAPTURE_GAMMA_CONSTANT,
    0,
  );
  const captureGammaTenthValueDistanceM = Math.max(
    input.captureGammaTenthValueDistanceM ?? MAZE_CAPTURE_GAMMA_TVD_METERS,
    0.1,
  );

  const innerMazeNeutronFluence =
    input.neutronSourceStrengthPerGray *
    (1 / (4 * Math.PI * Math.pow(input.innerMazeDistanceM, 2)) +
      (roomReturnFactor * headMaterialFactor) /
        (2 * Math.PI * input.roomSurfaceAreaM2) +
      apertureReturnFactor / (2 * Math.PI * input.roomSurfaceAreaM2));
  const captureGammaDoseEquivalentPerGray =
    captureGammaConstant *
    innerMazeNeutronFluence *
    Math.pow(10, -input.mazeLengthM / captureGammaTenthValueDistanceM);

  return {
    innerMazeNeutronFluence,
    captureGammaDoseEquivalentPerGray,
    weeklyCaptureGammaDoseEquivalent:
      input.leakageWorkload * captureGammaDoseEquivalentPerGray,
  };
}

export function calculateLinacMazeNeutronDoorDose(
  input                           ,
)                             {
  if (input.method === "kersey") {
    const neutronDoseEquivalentPerGrayAtDoor = Math.max(
      input.neutronDoseEquivalentPerGrayAtDoor ?? 0,
      0,
    );

    return {
      neutronDoseEquivalentPerGrayAtDoor,
      weeklyNeutronDoseEquivalent:
        input.leakageWorkload * neutronDoseEquivalentPerGrayAtDoor,
    };
  }

  const neutronFluenceAtInnerMazePoint = Math.max(
    input.neutronFluenceAtInnerMazePoint ?? 0,
    0,
  );
  const innerMazeOpeningAreaM2 = Math.max(
    input.innerMazeOpeningAreaM2 ?? 0,
    1e-9,
  );
  const mazeCrossSectionAreaM2 = Math.max(
    input.mazeCrossSectionAreaM2 ?? 0,
    1e-9,
  );
  const mazeLengthM = Math.max(input.mazeLengthM ?? 0, 0);
  const empiricalConstant = Math.max(
    input.empiricalConstant ?? MAZE_NEUTRON_WU_CONSTANT,
    0,
  );
  const tenthValueDistanceM =
    MAZE_NEUTRON_TVD_FACTOR * Math.sqrt(mazeCrossSectionAreaM2);
  const neutronDoseEquivalentPerGrayAtDoor =
    empiricalConstant *
    neutronFluenceAtInnerMazePoint *
    Math.sqrt(innerMazeOpeningAreaM2 / mazeCrossSectionAreaM2) *
    Math.pow(10, -mazeLengthM / Math.max(tenthValueDistanceM, 0.1));

  return {
    tenthValueDistanceM,
    neutronDoseEquivalentPerGrayAtDoor,
    weeklyNeutronDoseEquivalent:
      input.leakageWorkload * neutronDoseEquivalentPerGrayAtDoor,
  };
}

export function calculateLinacMazeDoorShielding(
  input                             ,
)                               {
  const halfGoal = Math.max(input.designGoal / 2, 1e-9);
  const neutronTvlMm = Math.max(
    input.neutronTvlMm ?? MAZE_DOOR_NEUTRON_TVL_MM,
    1e-9,
  );
  const captureGammaLeadTvlMm = Math.max(
    input.captureGammaLeadTvlMm ?? MAZE_DOOR_CAPTURE_GAMMA_LEAD_TVL_MM,
    1e-9,
  );

  return {
    halfGoal,
    leadThicknessMm:
      input.captureGammaDoseEquivalent <= halfGoal
        ? 0
        : captureGammaLeadTvlMm *
          Math.log10(input.captureGammaDoseEquivalent / halfGoal),
    bpeThicknessMm:
      input.neutronDoseEquivalent <= halfGoal
        ? 0
        : neutronTvlMm * Math.log10(input.neutronDoseEquivalent / halfGoal),
  };
}

export function calculateLinacMazeBarrierBenchmark(
  input                                ,
)                                  {
  const occupancyAdjustedGoal =
    input.designGoal / Math.max(input.occupancyFactor, 0.001);
  const angleOfIncidence = (input.angleOfIncidenceDegrees * Math.PI) / 180;
  const contributions                          = input.components.map(
    (component, index) => {
      const distanceMeters = Math.max(
        component.distanceMeters,
        MIN_DISTANCE_METERS,
      );
      const leakageFactor = clamp(component.leakageFactor ?? 1, 1, 10);
      const workloadAtOneMeter = component.weeklyWorkload * 10;
      const unshieldedAirKerma =
        (workloadAtOneMeter * LINAC_LEAKAGE_FRACTION * leakageFactor) /
        Math.pow(distanceMeters, 2);

      return {
        equipmentId: `maze-benchmark-eq-${index}`,
        equipmentName: `Maze barrier ${component.energyMV} MV`,
        sourceId: `maze-benchmark-src-${index}`,
        sourceName: `Leakage ${component.energyMV} MV`,
        sourceType: "leakage",
        calculationStandard: "NCRP 151 / IPEM 75",
        linacBeamEnergyMV: component.energyMV,
        linacWeeklyWorkload: component.weeklyWorkload,
        linacLeakageFactor: leakageFactor,
        unshieldedAirKerma,
        transmissionModel: "linac-tvl",
        linacTvl: getLinacTvlParams(
          input.material,
          component.energyMV,
          "leakage",
        ),
        angleOfIncidence,
      };
    },
  );
  const photonRequiredThickness = solveLinacSecondaryThickness(
    contributions,
    occupancyAdjustedGoal,
    "oblique",
  );
  const photonRequiredSlantThickness =
    photonRequiredThickness / Math.max(Math.cos(angleOfIncidence), 0.1);
  const recommendedSlantThickness = Math.max(
    photonRequiredSlantThickness,
    input.minimumSlantThickness ?? 0,
  );
  const recommendedThickness =
    recommendedSlantThickness * Math.max(Math.cos(angleOfIncidence), 0.1);

  return {
    photonRequiredThickness,
    photonRequiredSlantThickness,
    recommendedThickness,
    recommendedSlantThickness,
    occupancyAdjustedGoal,
    shieldedAirKermaAtRecommendedThickness:
      calculateShieldedAirKermaForThickness(
        contributions,
        recommendedThickness,
      ),
    contributions: contributions.map((contribution) => {
      const requiredTransmission = clamp(
        occupancyAdjustedGoal /
          Math.max(contribution.unshieldedAirKerma, 1e-12),
        1e-12,
        1,
      );
      const requiredThickness = calculateLinacRequiredThickness(
        requiredTransmission,
        contribution.linacTvl ,
        contribution.angleOfIncidence,
      );

      return {
        energyMV: contribution.linacBeamEnergyMV ?? 0,
        requiredThickness,
        requiredSlantThickness:
          requiredThickness /
          Math.max(Math.cos(contribution.angleOfIncidence), 0.1),
        unshieldedAirKerma: contribution.unshieldedAirKerma,
      };
    }),
  };
}

function calculateLinacUnshieldedAirKerma(
  equipment           ,
  component                 ,
  isocenterDistance        ,
)         {
  const effectiveDistance = Math.max(isocenterDistance, MIN_DISTANCE_METERS);
  const workloadAtOneMeter = equipment.workload * 10;
  const energyMV = getLinacEnergyMV(equipment);

  switch (component.type) {
    case "primary":
      return (
        (workloadAtOneMeter * Math.max(component.relativeOutput, 0.01)) /
        Math.pow(effectiveDistance + LINAC_ISOCENTER_DISTANCE_METERS, 2)
      );
    case "scatter":
      return (
        (workloadAtOneMeter *
          getLinacScatterUseFactor(equipment, component) *
          getLinacScatterFraction(
            energyMV,
            getLinacScatterAngleDegrees(equipment),
          ) *
          getLinacScatterFieldFactor(equipment)) /
        Math.pow(effectiveDistance, 2)
      );
    case "leakage":
      return (
        (workloadAtOneMeter *
          getLinacLeakageFraction(component) *
          getLinacLeakageFactor(equipment)) /
        Math.pow(effectiveDistance, 2)
      );
  }
}

function calculateComponentUnshieldedAirKerma(
  equipment           ,
  component                 ,
  isocenterDistance        ,
)         {
  return isLinacEquipment(equipment)
    ? calculateLinacUnshieldedAirKerma(equipment, component, isocenterDistance)
    : calculateUnshieldedAirKerma(
        equipment,
        isocenterDistance,
        component.relativeOutput,
      );
}

function getSourceDirectionVector(component                 )   
            
            
            
  {
  const azimuth = (component.directionDegrees * Math.PI) / 180;
  const elevation = (component.elevationDegrees * Math.PI) / 180;
  const horizontalMagnitude = Math.cos(elevation);

  return {
    x: Math.cos(azimuth) * horizontalMagnitude,
    y: Math.sin(azimuth) * horizontalMagnitude,
    z: Math.sin(elevation),
  };
}

function beamIncludesTarget(
  component                 ,
  targetX        ,
  targetY        ,
  targetZ        ,
)          {
  if (component.type !== "primary" || component.beamWidthDegrees >= 359) {
    return true;
  }

  const rayX = targetX - component.x;
  const rayY = targetY - component.y;
  const rayZ = targetZ - component.z;
  const rayLength = Math.sqrt(rayX * rayX + rayY * rayY + rayZ * rayZ);

  if (rayLength < 1e-9) {
    return true;
  }

  const beamVector = getSourceDirectionVector(component);
  const dot =
    (beamVector.x * rayX + beamVector.y * rayY + beamVector.z * rayZ) /
    rayLength;
  const angle = Math.acos(clamp(dot, -1, 1));

  return angle <= ((component.beamWidthDegrees / 2) * Math.PI) / 180;
}

export function calculateUnshieldedAirKerma(
  equipment           ,
  distance        ,
  relativeOutput         = equipment.useFactor,
)         {
  if (isLinacEquipment(equipment)) {
    const effectiveDistance = Math.max(distance, MIN_DISTANCE_METERS);
    return (
      (equipment.workload * 10 * relativeOutput) /
      Math.pow(effectiveDistance, 2)
    );
  }

  const effectiveDistance = Math.max(distance, MIN_DISTANCE_METERS);

  return (
    (equipment.K1 * equipment.nPatients * relativeOutput) /
    Math.pow(effectiveDistance, 2)
  );
}

function getTransmissionParams(
  material              ,
  kVp        ,
  isScatter          = false,
  workloadDist         ,
)                     {
  if (isScatter && workloadDist) {
    const category = getScatterCategory(workloadDist       );
    const scatterParams = SECONDARY_TRANSMISSION_PARAMS[material]?.[category];
    if (scatterParams) {
      return scatterParams;
    }
  }

  const kvpKey = getKvpKey(kVp);
  const primaryParams = PRIMARY_TRANSMISSION_PARAMS[material]?.[kvpKey];

  return (
    primaryParams ||
    PRIMARY_TRANSMISSION_PARAMS[material]?.["100kVp"] || {
      alpha: 0.1,
      beta: 0.01,
      gamma: 0.7,
    }
  );
}

function getTransmissionParamsForComponent(
  component                 ,
  equipment           ,
  material              ,
) 
                                                               
                                                                 {
  if (isLinacEquipment(equipment)) {
    return {
      transmissionModel: "linac-tvl",
      linacTvl: getLinacTvlParams(
        material,
        getLinacEnergyMV(equipment),
        component.type,
        getLinacScatterAngleDegrees(equipment),
      ),
    };
  }

  if (component.type === "scatter") {
    return {
      transmissionModel: "fitted",
      params: getTransmissionParams(
        material,
        equipment.kVp,
        true,
        component.workloadDistribution,
      ),
    };
  }

  if (component.type === "leakage") {
    return {
      transmissionModel: "fitted",
      params: getTransmissionParams(
        material,
        150,
        false,
        component.workloadDistribution,
      ),
    };
  }

  return {
    transmissionModel: "fitted",
    params: getTransmissionParams(
      material,
      equipment.kVp,
      false,
      component.workloadDistribution,
    ),
  };
}

function getOccupancyProfileFromRegion(
  region                ,
)                   {
  const occupancyFactor = Math.max(region.occupancyFactor, 0.05);

  return {
    id: region.id,
    name: region.name,
    designGoal: region.designGoal,
    occupancyFactor,
    occupancyAdjustedGoal: region.designGoal / occupancyFactor,
  };
}

function getOccupancyProfileFromWorkstation(
  workstation             ,
)                   {
  const designGoal = workstation.isControlled
    ? DOSE_LIMITS.controlled
    : DOSE_LIMITS.uncontrolled;
  const occupancyFactor = Math.max(workstation.occupancyFactor, 0.05);

  return {
    id: workstation.id,
    name: workstation.name,
    designGoal,
    occupancyFactor,
    occupancyAdjustedGoal: designGoal / occupancyFactor,
  };
}

function getMostRestrictiveProfile(
  profiles                    ,
)                          {
  if (profiles.length === 0) return null;

  return profiles.reduce((worst, current) =>
    current.occupancyAdjustedGoal < worst.occupancyAdjustedGoal
      ? current
      : worst,
  );
}

function calculateSideOfWall(point         , wall      )         {
  return crossProduct(wall.x1, wall.y1, wall.x2, wall.y2, point.x, point.y);
}

function getWallNormal(wall      )          {
  const dx = wall.x2 - wall.x1;
  const dy = wall.y2 - wall.y1;
  const length = Math.sqrt(dx * dx + dy * dy);

  if (length < 1e-9) {
    return { x: 0, y: 0 };
  }

  return {
    x: -dy / length,
    y: dx / length,
  };
}

function calculateIncidenceAngle(
  sourcePoint         ,
  samplePoint         ,
  wall      ,
)         {
  const normal = getWallNormal(wall);
  const rayX = samplePoint.x - sourcePoint.x;
  const rayY = samplePoint.y - sourcePoint.y;
  const rayLength = Math.sqrt(rayX * rayX + rayY * rayY);

  if (rayLength < 1e-9) {
    return 0;
  }

  const dot = (rayX * normal.x + rayY * normal.y) / rayLength;
  return Math.acos(clamp(Math.abs(dot), 0, 1));
}

function calculateShieldedAirKermaForThickness(
  contributions                         ,
  thickness        ,
)         {
  return contributions.reduce((sum, contribution) => {
    const transmission =
      contribution.transmissionModel === "linac-tvl" && contribution.linacTvl
        ? calculateLinacTransmissionFactor(
            thickness,
            contribution.linacTvl,
            contribution.angleOfIncidence,
          )
        : calculateTransmissionFactor(
            thickness,
            contribution.params ?? {
              alpha: 0.1,
              beta: 0.01,
              gamma: 0.7,
            },
            contribution.angleOfIncidence,
          );
    return sum + contribution.unshieldedAirKerma * transmission;
  }, 0);
}

function shouldUseLinacSecondaryRule(
  contributions                         ,
)          {
  return (
    contributions.length > 0 &&
    contributions.every(
      (contribution) =>
        contribution.transmissionModel === "linac-tvl" &&
        contribution.sourceType !== "primary" &&
        Boolean(contribution.linacTvl),
    )
  );
}

function getLinacCombinationRule(
  contributions                         ,
  preferredRule                                  = "auto",
)                         {
  if (preferredRule !== "auto") {
    return preferredRule;
  }

  const hasObliqueIncidence = contributions.some(
    (contribution) => contribution.angleOfIncidence > (1 * Math.PI) / 180,
  );

  return hasObliqueIncidence ? "oblique" : "standard";
}

function getSlantThickness(
  thickness        ,
  angleOfIncidence        ,
)         {
  return thickness / Math.max(Math.cos(angleOfIncidence), 0.1);
}

function getNormalThicknessFromSlant(
  slantThickness        ,
  angleOfIncidence        ,
)         {
  return slantThickness * Math.max(Math.cos(angleOfIncidence), 0.1);
}

function combineComparableLinacRequirements(
  requirements         
                                    
                              
                         
                
    ,
)                                                                          {
  if (requirements.length === 0) {
    return null;
  }

  const sorted = [...requirements].sort(
    (left, right) => right.requiredThickness - left.requiredThickness,
  );
  let combinedThickness = sorted[0].requiredThickness;
  let referenceTvl = sorted[0].referenceTvl;
  let hvl = sorted[0].hvl;

  for (let index = 1; index < sorted.length; index++) {
    const requirement = sorted[index];
    const comparableDelta = Math.max(referenceTvl, requirement.referenceTvl);

    if (combinedThickness - requirement.requiredThickness <= comparableDelta) {
      combinedThickness += Math.max(hvl, requirement.hvl);
      referenceTvl = Math.max(referenceTvl, requirement.referenceTvl);
      hvl = Math.max(hvl, requirement.hvl);
    }
  }

  return {
    requiredThickness: combinedThickness,
    referenceTvl,
    hvl,
  };
}

function solveLinacSecondaryThickness(
  contributions                         ,
  occupancyAdjustedGoal        ,
  preferredRule                                  = "auto",
)         {
  const combinationRule = getLinacCombinationRule(contributions, preferredRule);
  const requirements = contributions
    .filter(
      (
        contribution,
                                                  
                                       
                                 
      ) =>
        contribution.transmissionModel === "linac-tvl" &&
        Boolean(contribution.linacTvl),
    )
    .map((contribution) => {
      const requiredTransmission = clamp(
        occupancyAdjustedGoal /
          Math.max(contribution.unshieldedAirKerma, 1e-12),
        1e-12,
        1,
      );

      return {
        sourceType: contribution.sourceType,
        angleOfIncidence: contribution.angleOfIncidence,
        requiredThickness: calculateLinacRequiredThickness(
          requiredTransmission,
          contribution.linacTvl,
          contribution.angleOfIncidence,
        ),
        referenceTvl: contribution.linacTvl.referenceTvl,
        hvl: contribution.linacTvl.hvl,
      };
    })
    .sort((left, right) => right.requiredThickness - left.requiredThickness);

  if (requirements.length === 0) {
    return 0;
  }

  if (combinationRule === "oblique") {
    const slantRequirements = requirements
      .map((requirement) => ({
        ...requirement,
        requiredSlantThickness: getSlantThickness(
          requirement.requiredThickness,
          requirement.angleOfIncidence,
        ),
      }))
      .sort(
        (left, right) =>
          right.requiredSlantThickness - left.requiredSlantThickness,
      );
    const dominantRequirement = slantRequirements[0];
    let combinedSlantThickness = dominantRequirement.requiredSlantThickness;
    const secondRequirement = slantRequirements[1];

    if (
      secondRequirement &&
      combinedSlantThickness - secondRequirement.requiredSlantThickness <=
        dominantRequirement.referenceTvl
    ) {
      combinedSlantThickness += dominantRequirement.hvl;
    }

    let combinedThickness = getNormalThicknessFromSlant(
      combinedSlantThickness,
      dominantRequirement.angleOfIncidence,
    );

    while (
      calculateShieldedAirKermaForThickness(contributions, combinedThickness) >
        occupancyAdjustedGoal &&
      combinedThickness < MAX_THICKNESS_MM
    ) {
      combinedSlantThickness += dominantRequirement.hvl;
      combinedThickness = getNormalThicknessFromSlant(
        combinedSlantThickness,
        dominantRequirement.angleOfIncidence,
      );
    }

    return clamp(combinedThickness, 0, MAX_THICKNESS_MM);
  }

  const groupedRequirements = Array.from(
    requirements
      .reduce(
        (groups, requirement) => {
          const key = requirement.sourceType;
          const existing = groups.get(key) ?? [];
          existing.push(requirement);
          groups.set(key, existing);
          return groups;
        },
        new Map 
                              
                 
                                            
                                      
                                 
                        
            
         (),
      )
      .values(),
  )
    .map((group) => combineComparableLinacRequirements(group))
    .filter(
      (
        group,
                   
                                  
                             
                    
      ) => Boolean(group),
    )
    .sort((left, right) => right.requiredThickness - left.requiredThickness);

  const combinedRequirements = combineComparableLinacRequirements(
    groupedRequirements.map((group) => ({
      sourceType: "scatter",
      requiredThickness: group.requiredThickness,
      referenceTvl: group.referenceTvl,
      hvl: group.hvl,
    })),
  );

  return clamp(
    combinedRequirements?.requiredThickness ?? 0,
    0,
    MAX_THICKNESS_MM,
  );
}

function solveRequiredThicknessForContributions(
  contributions                         ,
  occupancyAdjustedGoal        ,
  maxIterations         = 60,
  tolerance         = 1e-14,
)         {
  if (contributions.length === 0) {
    return 0;
  }

  const unshieldedTotal = contributions.reduce(
    (sum, contribution) => sum + contribution.unshieldedAirKerma,
    0,
  );

  if (unshieldedTotal <= occupancyAdjustedGoal) {
    return 0;
  }

  if (shouldUseLinacSecondaryRule(contributions)) {
    return solveLinacSecondaryThickness(contributions, occupancyAdjustedGoal);
  }

  let xLow = 0;
  let xHigh = 10;

  while (
    calculateShieldedAirKermaForThickness(contributions, xHigh) >
      occupancyAdjustedGoal &&
    xHigh < MAX_THICKNESS_MM
  ) {
    xHigh *= 2;
  }

  xHigh = Math.min(xHigh, MAX_THICKNESS_MM);

  for (let iteration = 0; iteration < maxIterations; iteration++) {
    const xMid = (xLow + xHigh) / 2;
    const transmitted = calculateShieldedAirKermaForThickness(
      contributions,
      xMid,
    );
    const error = transmitted - occupancyAdjustedGoal;

    if (Math.abs(error) < tolerance) {
      return xMid;
    }

    if (error > 0) {
      xLow = xMid;
    } else {
      xHigh = xMid;
    }
  }

  return (xLow + xHigh) / 2;
}

function summarizeContributions(contributions                         )   
                         
                           
                              
                             
                               
                            
                                    
                              
                          
  {
  const equipmentIds = Array.from(
    new Set(contributions.map((item) => item.equipmentId)),
  );
  const equipmentNames = Array.from(
    new Set(contributions.map((item) => item.equipmentName)),
  );
  const standards = Array.from(
    new Set(contributions.map((item) => item.calculationStandard)),
  );
  const firstLinacContribution = contributions.find(
    (item) => item.calculationStandard === "NCRP 151 / IPEM 75",
  );
  return {
    equipmentIds,
    equipmentNames,
    calculationStandard:
      standards.length === 1
        ? standards[0]
        : "Mixed: NCRP 147 + NCRP 151 / IPEM 75",
    linacBeamEnergyMV: firstLinacContribution?.linacBeamEnergyMV,
    linacWeeklyWorkload: firstLinacContribution?.linacWeeklyWorkload,
    linacFieldSizeCm: firstLinacContribution?.linacFieldSizeCm,
    linacScatterAngleDegrees: firstLinacContribution?.linacScatterAngleDegrees,
    linacLeakageFactor: firstLinacContribution?.linacLeakageFactor,
    minimumDistance: Number.POSITIVE_INFINITY,
  };
}

function evaluateContributionsAtThickness(
  contributions                         ,
  thickness        ,
  occupancy                  ,
  distance        ,
)                                              {
  if (contributions.length === 0) {
    return null;
  }

  const unshieldedAirKerma = contributions.reduce(
    (sum, contribution) => sum + contribution.unshieldedAirKerma,
    0,
  );
  const shieldedAirKerma = calculateShieldedAirKermaForThickness(
    contributions,
    thickness,
  );
  const summary = summarizeContributions(contributions);

  return {
    distance,
    occupancy,
    calculationStandard: summary.calculationStandard,
    linacBeamEnergyMV: summary.linacBeamEnergyMV,
    linacWeeklyWorkload: summary.linacWeeklyWorkload,
    linacFieldSizeCm: summary.linacFieldSizeCm,
    linacScatterAngleDegrees: summary.linacScatterAngleDegrees,
    linacLeakageFactor: summary.linacLeakageFactor,
    unshieldedAirKerma,
    shieldedAirKerma,
    requiredThickness: solveRequiredThicknessForContributions(
      contributions,
      occupancy.occupancyAdjustedGoal,
    ),
    requiredTransmission: Math.min(
      occupancy.occupancyAdjustedGoal / Math.max(unshieldedAirKerma, 1e-12),
      1,
    ),
    currentTransmission: clamp(
      shieldedAirKerma / Math.max(unshieldedAirKerma, 1e-12),
      0,
      1,
    ),
    activeEquipmentIds: summary.equipmentIds,
    activeEquipmentNames: summary.equipmentNames,
  };
}

function getWallSampleOccupancy(
  room      ,
  wall      ,
  side        ,
  point         ,
)                          {
  const matchingWallRegions = room.occupiedRegions
    .filter(
      (region) =>
        region.scope === "wall" && pointInPolygon(point, region.points),
    )
    .map(getOccupancyProfileFromRegion);

  if (matchingWallRegions.length > 0) {
    return getMostRestrictiveProfile(matchingWallRegions);
  }

  const hasExplicitWallRegions = room.occupiedRegions.some(
    (region) => region.scope === "wall",
  );
  if (hasExplicitWallRegions) {
    return null;
  }

  const candidateWorkstations = room.workstations.filter((workstation) => {
    const sideValue = calculateSideOfWall(
      { x: workstation.x, y: workstation.y },
      wall,
    );
    return side === 1 ? sideValue > 0 : sideValue < 0;
  });

  return getMostRestrictiveProfile(
    candidateWorkstations.map(getOccupancyProfileFromWorkstation),
  );
}

function getHorizontalSampleOccupancy(
  room      ,
  point         ,
  scope                     ,
)                          {
  const matchingRegions = room.occupiedRegions
    .filter(
      (region) =>
        region.scope === scope && pointInPolygon(point, region.points),
    )
    .map(getOccupancyProfileFromRegion);

  if (matchingRegions.length > 0) {
    return getMostRestrictiveProfile(matchingRegions);
  }

  const hasExplicitRegions = room.occupiedRegions.some(
    (region) => region.scope === scope,
  );
  if (hasExplicitRegions) {
    return null;
  }

  return getMostRestrictiveProfile(
    room.workstations.map(getOccupancyProfileFromWorkstation),
  );
}

function getWallContributions(
  room      ,
  wall      ,
  side        ,
  samplePoint         ,
)                                                                      {
  const contributions                          = [];
  let nearestDistance = Number.POSITIVE_INFINITY;

  for (const equipment of room.equipment) {
    for (const component of getEquipmentSourceComponents(equipment)) {
      if (!component.enabled) continue;

      const sourceSide = calculateSideOfWall(
        { x: component.x, y: component.y },
        wall,
      );
      const oppositeSide = side === 1 ? sourceSide < 0 : sourceSide > 0;
      if (!oppositeSide) continue;
      if (
        !beamIncludesTarget(
          component,
          samplePoint.x,
          samplePoint.y,
          component.z,
        )
      )
        continue;

      const distance = calculateDistance(
        component.x,
        component.y,
        samplePoint.x,
        samplePoint.y,
      );
      nearestDistance = Math.min(nearestDistance, distance);
      contributions.push({
        equipmentId: equipment.id,
        equipmentName: equipment.name,
        sourceId: component.id,
        sourceName: component.name,
        sourceType: component.type,
        calculationStandard: getCalculationStandardForEquipment(equipment),
        ...getLinacScatterMetadata(equipment),
        unshieldedAirKerma: calculateComponentUnshieldedAirKerma(
          equipment,
          component,
          calculateDistance(
            equipment.x,
            equipment.y,
            samplePoint.x,
            samplePoint.y,
          ),
        ),
        ...getTransmissionParamsForComponent(
          component,
          equipment,
          wall.material,
        ),
        angleOfIncidence: calculateIncidenceAngle(
          { x: component.x, y: component.y },
          samplePoint,
          wall,
        ),
      });
    }
  }

  return { contributions, nearestDistance };
}

function getHorizontalContributions(
  room      ,
  barrierMaterial              ,
  point         ,
  targetZ        ,
)                                                                      {
  const contributions                          = [];
  let nearestDistance = Number.POSITIVE_INFINITY;

  for (const equipment of room.equipment) {
    for (const component of getEquipmentSourceComponents(equipment)) {
      if (!component.enabled) continue;
      if (!beamIncludesTarget(component, point.x, point.y, targetZ)) continue;

      const distance = calculate3DDistance(
        component.x,
        component.y,
        component.z,
        point.x,
        point.y,
        targetZ,
      );
      nearestDistance = Math.min(nearestDistance, distance);
      contributions.push({
        equipmentId: equipment.id,
        equipmentName: equipment.name,
        sourceId: component.id,
        sourceName: component.name,
        sourceType: component.type,
        calculationStandard: getCalculationStandardForEquipment(equipment),
        ...getLinacScatterMetadata(equipment),
        unshieldedAirKerma: calculateComponentUnshieldedAirKerma(
          equipment,
          component,
          calculate3DDistance(
            equipment.x,
            equipment.y,
            equipment.z,
            point.x,
            point.y,
            targetZ,
          ),
        ),
        ...getTransmissionParamsForComponent(
          component,
          equipment,
          barrierMaterial,
        ),
        angleOfIncidence: 0,
      });
    }
  }

  return { contributions, nearestDistance };
}

function sampleWallSide(
  room      ,
  wall      ,
  side        ,
  sampleDistance        ,
  sampleDensity         = DEFAULT_WALL_SAMPLE_DENSITY_METERS,
)                        {
  const results                        = [];
  const wallDx = wall.x2 - wall.x1;
  const wallDy = wall.y2 - wall.y1;
  const wallLength = Math.sqrt(wallDx * wallDx + wallDy * wallDy);

  if (wallLength < 0.01) {
    return results;
  }

  const normal = getWallNormal(wall);
  const numSamples = Math.max(1, Math.ceil(wallLength / sampleDensity));

  for (let sampleIndex = 0; sampleIndex <= numSamples; sampleIndex++) {
    const t = sampleIndex / numSamples;
    const point = {
      x: wall.x1 + t * wallDx + normal.x * sampleDistance * side,
      y: wall.y1 + t * wallDy + normal.y * sampleDistance * side,
    };
    const occupancy = getWallSampleOccupancy(room, wall, side, point);
    if (!occupancy) continue;

    const { contributions, nearestDistance } = getWallContributions(
      room,
      wall,
      side,
      point,
    );
    const evaluated = evaluateContributionsAtThickness(
      contributions,
      wall.thickness,
      occupancy,
      nearestDistance,
    );

    if (evaluated) {
      results.push({ x: point.x, y: point.y, ...evaluated });
    }
  }

  return results;
}

function evaluateWallSide(
  room      ,
  wall      ,
  side        ,
)                            {
  const samples = sampleWallSide(
    room,
    wall,
    side,
    room.sampleDistance || MIN_DISTANCE_METERS,
  );

  if (samples.length === 0) {
    return null;
  }

  const worstPoint = samples.reduce((worst, current) => {
    if (current.requiredThickness > worst.requiredThickness) return current;
    if (
      current.requiredThickness === worst.requiredThickness &&
      current.shieldedAirKerma > worst.shieldedAirKerma
    ) {
      return current;
    }
    return worst;
  });

  return { wall, side, worstPoint };
}

function createWallResult(evaluation                    )                  {
  const { wall, side, worstPoint } = evaluation;

  return {
    workstationId: worstPoint.occupancy.id,
    workstationName: `${worstPoint.occupancy.name} - ${wall.id} ${side === 1 ? "Side A" : "Side B"}`,
    equipmentId: worstPoint.activeEquipmentIds.join(","),
    equipmentName:
      worstPoint.activeEquipmentNames.length > 1
        ? `${worstPoint.activeEquipmentNames.length} sources`
        : worstPoint.activeEquipmentNames[0] || "No source",
    calculationStandard: worstPoint.calculationStandard,
    linacBeamEnergyMV: worstPoint.linacBeamEnergyMV,
    linacWeeklyWorkload: worstPoint.linacWeeklyWorkload,
    linacFieldSizeCm: worstPoint.linacFieldSizeCm,
    linacScatterAngleDegrees: worstPoint.linacScatterAngleDegrees,
    linacLeakageFactor: worstPoint.linacLeakageFactor,
    distance: worstPoint.distance,
    unshieldedAirKerma: worstPoint.unshieldedAirKerma,
    requiredTransmission: worstPoint.requiredTransmission,
    currentTransmission: worstPoint.currentTransmission,
    shieldedAirKerma: worstPoint.shieldedAirKerma,
    designGoal: worstPoint.occupancy.designGoal,
    occupancyAdjustedGoal: worstPoint.occupancy.occupancyAdjustedGoal,
    passed: worstPoint.shieldedAirKerma <= worstPoint.occupancy.designGoal,
    requiredThickness: worstPoint.requiredThickness,
    additionalThickness: Math.max(
      0,
      worstPoint.requiredThickness - wall.thickness,
    ),
    barrierMaterial: wall.material,
  };
}

function sampleHorizontalBarrierPlane(
  room      ,
  barrierMaterial              ,
  thickness        ,
  targetZ        ,
  scope                     ,
  planeLabel                   ,
  sampleDensity         = DEFAULT_PLANE_SAMPLE_DENSITY_METERS,
)                    {
  const results                    = [];
  const regions = room.occupiedRegions.filter(
    (region) => region.scope === scope,
  );

  if (regions.length === 0 && room.workstations.length === 0) {
    return results;
  }

  const xSamples = Math.max(1, Math.ceil(room.width / sampleDensity));
  const ySamples = Math.max(1, Math.ceil(room.height / sampleDensity));
  const worstByOccupancy = new Map                             ();

  for (let ix = 0; ix <= xSamples; ix++) {
    for (let iy = 0; iy <= ySamples; iy++) {
      const point = {
        x: (ix / xSamples) * room.width,
        y: (iy / ySamples) * room.height,
      };
      const occupancy = getHorizontalSampleOccupancy(room, point, scope);
      if (!occupancy) continue;

      const { contributions, nearestDistance } = getHorizontalContributions(
        room,
        barrierMaterial,
        point,
        targetZ,
      );
      const evaluated = evaluateContributionsAtThickness(
        contributions,
        thickness,
        occupancy,
        nearestDistance,
      );

      if (!evaluated) continue;

      const sampledPoint                      = {
        x: point.x,
        y: point.y,
        ...evaluated,
      };
      const currentWorst = worstByOccupancy.get(occupancy.id);

      if (
        !currentWorst ||
        sampledPoint.requiredThickness > currentWorst.requiredThickness ||
        (sampledPoint.requiredThickness === currentWorst.requiredThickness &&
          sampledPoint.shieldedAirKerma > currentWorst.shieldedAirKerma)
      ) {
        worstByOccupancy.set(occupancy.id, sampledPoint);
      }
    }
  }

  for (const worstPoint of worstByOccupancy.values()) {
    results.push({
      workstationId: worstPoint.occupancy.id,
      workstationName: `${worstPoint.occupancy.name} (${planeLabel})`,
      equipmentId: worstPoint.activeEquipmentIds.join(","),
      equipmentName:
        worstPoint.activeEquipmentNames.length > 1
          ? `${worstPoint.activeEquipmentNames.length} sources`
          : worstPoint.activeEquipmentNames[0] || "No source",
      calculationStandard: worstPoint.calculationStandard,
      linacBeamEnergyMV: worstPoint.linacBeamEnergyMV,
      linacWeeklyWorkload: worstPoint.linacWeeklyWorkload,
      linacFieldSizeCm: worstPoint.linacFieldSizeCm,
      linacScatterAngleDegrees: worstPoint.linacScatterAngleDegrees,
      linacLeakageFactor: worstPoint.linacLeakageFactor,
      distance: worstPoint.distance,
      unshieldedAirKerma: worstPoint.unshieldedAirKerma,
      requiredTransmission: worstPoint.requiredTransmission,
      currentTransmission: worstPoint.currentTransmission,
      shieldedAirKerma: worstPoint.shieldedAirKerma,
      designGoal: worstPoint.occupancy.designGoal,
      occupancyAdjustedGoal: worstPoint.occupancy.occupancyAdjustedGoal,
      passed: worstPoint.shieldedAirKerma <= worstPoint.occupancy.designGoal,
      requiredThickness: worstPoint.requiredThickness,
      additionalThickness: Math.max(
        0,
        worstPoint.requiredThickness - thickness,
      ),
      barrierMaterial,
    });
  }

  return results;
}

export function calculateTotalTransmission(
  walls        ,
  kVp        ,
  workloadDist        ,
)         {
  if (walls.length === 0) return 1;

  let totalTransmission = 1;
  for (const wall of walls) {
    const params = getTransmissionParams(
      wall.material,
      kVp,
      true,
      workloadDist,
    );
    totalTransmission *= calculateTransmissionFactor(wall.thickness, params);
  }

  return totalTransmission;
}

export function calculateShielding(
  equipment           ,
  workstation             ,
  walls        ,
)                  {
  const distance = calculateDistance(
    equipment.x,
    equipment.y,
    workstation.x,
    workstation.y,
  );
  const intersectingWalls = getIntersectingWalls(equipment, workstation, walls);
  const occupancy = getOccupancyProfileFromWorkstation(workstation);
  const unshieldedAirKerma = calculateUnshieldedAirKerma(equipment, distance);
  const currentTransmission = calculateTotalTransmission(
    intersectingWalls,
    equipment.kVp,
    equipment.workloadDistribution,
  );
  const shieldedAirKerma = unshieldedAirKerma * currentTransmission;
  const requiredTransmission = Math.min(
    occupancy.occupancyAdjustedGoal / Math.max(unshieldedAirKerma, 1e-12),
    1,
  );
  const barrierMaterial = intersectingWalls[0]?.material || "lead";
  const requiredThickness = calculateRequiredThickness(
    requiredTransmission,
    getTransmissionParams(
      barrierMaterial,
      equipment.kVp,
      true,
      equipment.workloadDistribution,
    ),
  );
  const currentThickness = intersectingWalls.reduce(
    (sum, wall) => sum + wall.thickness,
    0,
  );

  return {
    workstationId: workstation.id,
    workstationName: workstation.name,
    equipmentId: equipment.id,
    equipmentName: equipment.name,
    calculationStandard: getCalculationStandardForEquipment(equipment),
    ...getLinacScatterMetadata(equipment),
    distance: Math.max(distance, MIN_DISTANCE_METERS),
    unshieldedAirKerma,
    requiredTransmission,
    currentTransmission,
    shieldedAirKerma,
    designGoal: occupancy.designGoal,
    occupancyAdjustedGoal: occupancy.occupancyAdjustedGoal,
    passed: shieldedAirKerma <= occupancy.designGoal,
    requiredThickness,
    additionalThickness: Math.max(0, requiredThickness - currentThickness),
    barrierMaterial,
  };
}

export function calculateFloorShielding(
  equipment           ,
  workstation             ,
  floorThickness        ,
  floorMaterial              ,
  roomHeight        ,
)                  {
  const occupancy = getOccupancyProfileFromWorkstation(workstation);
  const distance = calculate3DDistance(
    equipment.x,
    equipment.y,
    equipment.z,
    workstation.x,
    workstation.y,
    0,
  );
  const unshieldedAirKerma = calculateUnshieldedAirKerma(equipment, distance);
  const params = getTransmissionParams(
    floorMaterial,
    equipment.kVp,
    true,
    equipment.workloadDistribution,
  );
  const currentTransmission = calculateTransmissionFactor(
    floorThickness,
    params,
  );
  const shieldedAirKerma = unshieldedAirKerma * currentTransmission;
  const requiredTransmission = Math.min(
    occupancy.occupancyAdjustedGoal / Math.max(unshieldedAirKerma, 1e-12),
    1,
  );

  return {
    workstationId: workstation.id,
    workstationName: `${workstation.name} (Below)`,
    equipmentId: equipment.id,
    equipmentName: equipment.name,
    calculationStandard: getCalculationStandardForEquipment(equipment),
    ...getLinacScatterMetadata(equipment),
    distance,
    unshieldedAirKerma,
    requiredTransmission,
    currentTransmission,
    shieldedAirKerma,
    designGoal: occupancy.designGoal,
    occupancyAdjustedGoal: occupancy.occupancyAdjustedGoal,
    passed: shieldedAirKerma <= occupancy.designGoal,
    requiredThickness: calculateRequiredThickness(requiredTransmission, params),
    additionalThickness: Math.max(
      0,
      calculateRequiredThickness(requiredTransmission, params) - floorThickness,
    ),
    barrierMaterial: floorMaterial,
  };
}

export function calculateCeilingShielding(
  equipment           ,
  workstation             ,
  ceilingThickness        ,
  ceilingMaterial              ,
  roomHeight        ,
)                  {
  const occupancy = getOccupancyProfileFromWorkstation(workstation);
  const distance = calculate3DDistance(
    equipment.x,
    equipment.y,
    equipment.z,
    workstation.x,
    workstation.y,
    roomHeight,
  );
  const unshieldedAirKerma = calculateUnshieldedAirKerma(equipment, distance);
  const params = getTransmissionParams(
    ceilingMaterial,
    equipment.kVp,
    true,
    equipment.workloadDistribution,
  );
  const currentTransmission = calculateTransmissionFactor(
    ceilingThickness,
    params,
  );
  const shieldedAirKerma = unshieldedAirKerma * currentTransmission;
  const requiredTransmission = Math.min(
    occupancy.occupancyAdjustedGoal / Math.max(unshieldedAirKerma, 1e-12),
    1,
  );

  return {
    workstationId: workstation.id,
    workstationName: `${workstation.name} (Above)`,
    equipmentId: equipment.id,
    equipmentName: equipment.name,
    calculationStandard: getCalculationStandardForEquipment(equipment),
    ...getLinacScatterMetadata(equipment),
    distance,
    unshieldedAirKerma,
    requiredTransmission,
    currentTransmission,
    shieldedAirKerma,
    designGoal: occupancy.designGoal,
    occupancyAdjustedGoal: occupancy.occupancyAdjustedGoal,
    passed: shieldedAirKerma <= occupancy.designGoal,
    requiredThickness: calculateRequiredThickness(requiredTransmission, params),
    additionalThickness: Math.max(
      0,
      calculateRequiredThickness(requiredTransmission, params) -
        ceilingThickness,
    ),
    barrierMaterial: ceilingMaterial,
  };
}

export function sampleBarrierPoints(
  equipment             ,
  wall      ,
  sampleDistance         = MIN_DISTANCE_METERS,
  sampleDensity         = DEFAULT_WALL_SAMPLE_DENSITY_METERS,
)                                               {
  const results                                               = [];
  const wallDx = wall.x2 - wall.x1;
  const wallDy = wall.y2 - wall.y1;
  const wallLength = Math.sqrt(wallDx * wallDx + wallDy * wallDy);
  const normal = getWallNormal(wall);

  if (wallLength < 0.01) {
    return results;
  }

  const numSamples = Math.max(1, Math.ceil(wallLength / sampleDensity));

  for (let sampleIndex = 0; sampleIndex <= numSamples; sampleIndex++) {
    const t = sampleIndex / numSamples;
    const point = {
      x: wall.x1 + t * wallDx + normal.x * sampleDistance,
      y: wall.y1 + t * wallDy + normal.y * sampleDistance,
    };

    let totalAirKerma = 0;
    for (const eq of equipment) {
      for (const component of getEquipmentSourceComponents(eq)) {
        if (!component.enabled) continue;
        totalAirKerma += calculateComponentUnshieldedAirKerma(
          eq,
          component,
          calculateDistance(component.x, component.y, point.x, point.y),
        );
      }
    }

    results.push({ x: point.x, y: point.y, airKerma: totalAirKerma });
  }

  return results;
}

export function findMaxAirKermaLocation(
  room      ,
)                                                    {
  let maxResult                                                    = null;

  for (const wall of room.walls) {
    const samples = sampleBarrierPoints(
      room.equipment,
      wall,
      room.sampleDistance || MIN_DISTANCE_METERS,
    );

    for (const sample of samples) {
      if (!maxResult || sample.airKerma > maxResult.airKerma) {
        maxResult = sample;
      }
    }
  }

  return maxResult;
}

function generateLinacMazeAnalysisReport(
  config                         ,
)                          {
  const doorDesignGoalSvPerWeek = config.doorDesignGoalMsvPerWeek * 1e-3;
  const photonDose = calculateLinacMazeDoorPhotonDose({
    energies: config.energies.map((energy) => ({
      energyMV: energy.energyMV,
      weeklyWorkload: energy.primaryWeeklyWorkloadGy,
      headLeakageWeeklyWorkload: energy.leakageWeeklyWorkloadGy,
      transmittedLeakageWeeklyWorkload: energy.leakageWeeklyWorkloadGy,
      wallScatterCoefficient: energy.wallScatterCoefficient,
      headLeakageWallScatterCoefficient:
        energy.headLeakageWallScatterCoefficient,
      patientScatterFraction: energy.patientScatterFraction,
      transmittedLeakageBarrierTransmission:
        energy.transmittedLeakageBarrierTransmission,
    })),
    useFactor: config.useFactor,
    fieldSizeCm: config.fieldSizeCm,
    roomScatterCorrectionFactor: config.roomScatterCorrectionFactor,
    primaryWallFraction: config.primaryWallFraction,
    wallScatterProjectionAreaM2: config.wallScatterProjectionAreaM2,
    wallScatterReflectionCoefficient: config.wallScatterReflectionCoefficient,
    mazeEntranceProjectionAreaM2: config.mazeEntranceProjectionAreaM2,
    wallScatterSourceDistanceM: config.wallScatterSourceDistanceM,
    wallScatterMazeDistanceM: config.wallScatterMazeDistanceM,
    wallScatterDoorDistanceM: config.wallScatterDoorDistanceM,
    headLeakageScatterAreaM2: config.headLeakageScatterAreaM2,
    headLeakageScatterDistanceM: config.headLeakageScatterDistanceM,
    headLeakageDoorDistanceM: config.headLeakageDoorDistanceM,
    patientScatterWallReflectionCoefficient:
      config.patientScatterWallReflectionCoefficient,
    patientScatterDistanceM: config.patientScatterDistanceM,
    patientScatterMazeDistanceM: config.patientScatterMazeDistanceM,
    patientScatterDoorDistanceM: config.patientScatterDoorDistanceM,
    transmittedLeakageDistanceM: config.transmittedLeakageDistanceM,
  });
  const neutronEnergy =
    [...config.energies]
      .sort((left, right) => right.energyMV - left.energyMV)
      .find(
        (energy) =>
          typeof energy.neutronSourceStrengthPerGray === "number" &&
          typeof energy.neutronDoseEquivalentPerGrayAtDoor === "number",
      ) ?? config.energies[0];
  const captureGamma = calculateLinacMazeCaptureGammaDose({
    leakageWorkload: neutronEnergy?.leakageWeeklyWorkloadGy ?? 0,
    neutronSourceStrengthPerGray:
      neutronEnergy?.neutronSourceStrengthPerGray ?? 0,
    innerMazeDistanceM: config.innerMazeDistanceM,
    mazeLengthM: config.mazeLengthM,
    roomSurfaceAreaM2: config.roomSurfaceAreaM2,
  });
  const neutronDoorKersey = calculateLinacMazeNeutronDoorDose({
    method: "kersey",
    leakageWorkload: neutronEnergy?.leakageWeeklyWorkloadGy ?? 0,
    neutronDoseEquivalentPerGrayAtDoor:
      neutronEnergy?.neutronDoseEquivalentPerGrayAtDoor ?? 0,
  });
  const neutronDoorWu = calculateLinacMazeNeutronDoorDose({
    method: "wu-mcginley",
    leakageWorkload: neutronEnergy?.leakageWeeklyWorkloadGy ?? 0,
    neutronFluenceAtInnerMazePoint: captureGamma.innerMazeNeutronFluence,
    innerMazeOpeningAreaM2: config.innerMazeOpeningAreaM2,
    mazeCrossSectionAreaM2: config.mazeCrossSectionAreaM2,
    mazeLengthM: config.mazeLengthM,
  });
  const mazeBarrier = calculateLinacMazeBarrierBenchmark({
    material: config.mazeBarrierMaterial,
    designGoal: config.doorDesignGoalMsvPerWeek,
    occupancyFactor: 1,
    angleOfIncidenceDegrees: config.mazeBarrierAngleDegrees,
    minimumSlantThickness: config.minimumMazeBarrierSlantThicknessMm,
    components: config.energies.map((energy) => ({
      energyMV: energy.energyMV,
      weeklyWorkload: energy.leakageWeeklyWorkloadGy * 100,
      distanceMeters: config.mazeBarrierDistanceM,
    })),
  });
  const doorShielding = calculateLinacMazeDoorShielding({
    designGoal: doorDesignGoalSvPerWeek,
    neutronDoseEquivalent: neutronDoorKersey.weeklyNeutronDoseEquivalent,
    captureGammaDoseEquivalent: captureGamma.weeklyCaptureGammaDoseEquivalent,
  });

  return {
    scenarioName: config.scenarioName,
    doorDesignGoalSvPerWeek,
    photonDoseEquivalent: photonDose.totalPhotonDoseEquivalent,
    photonComponents: {
      wallScatterDoseEquivalent: photonDose.wallScatterDoseEquivalent,
      headLeakageWallScatterDoseEquivalent:
        photonDose.headLeakageWallScatterDoseEquivalent,
      patientScatterDoseEquivalent: photonDose.patientScatterDoseEquivalent,
      transmittedLeakageDoseEquivalent:
        photonDose.transmittedLeakageDoseEquivalent,
    },
    captureGammaDoseEquivalent: captureGamma.weeklyCaptureGammaDoseEquivalent,
    neutronDoseEquivalentKersey: neutronDoorKersey.weeklyNeutronDoseEquivalent,
    neutronDoseEquivalentWu: neutronDoorWu.weeklyNeutronDoseEquivalent,
    totalDoseEquivalentKersey:
      photonDose.totalPhotonDoseEquivalent +
      captureGamma.weeklyCaptureGammaDoseEquivalent +
      neutronDoorKersey.weeklyNeutronDoseEquivalent,
    totalDoseEquivalentWu:
      photonDose.totalPhotonDoseEquivalent +
      captureGamma.weeklyCaptureGammaDoseEquivalent +
      neutronDoorWu.weeklyNeutronDoseEquivalent,
    mazeBarrierRequiredThicknessMm: mazeBarrier.recommendedThickness,
    mazeBarrierRequiredSlantThicknessMm: mazeBarrier.recommendedSlantThickness,
    installedMazeBarrierThicknessMm: config.installedMazeBarrierThicknessMm,
    mazeBarrierPassed:
      config.installedMazeBarrierThicknessMm >=
      mazeBarrier.recommendedThickness,
    doorShieldingRequiredLeadMm: doorShielding.leadThicknessMm,
    doorShieldingRequiredBpeMm: doorShielding.bpeThicknessMm,
    installedDoorLeadMm: config.installedDoorLeadMm,
    installedDoorBpeMm: config.installedDoorBpeMm,
    doorShieldingPassed:
      config.installedDoorLeadMm >= doorShielding.leadThicknessMm &&
      config.installedDoorBpeMm >= doorShielding.bpeThicknessMm,
  };
}

export function generateQAReport(room      )           {
  const results                    = [];

  for (const wall of room.walls) {
    const sideA = evaluateWallSide(room, wall, 1);
    const sideB = evaluateWallSide(room, wall, -1);

    if (sideA) results.push(createWallResult(sideA));
    if (sideB) results.push(createWallResult(sideB));
  }

  const floorResults = sampleHorizontalBarrierPlane(
    room,
    room.floor.material,
    room.floor.thickness,
    0,
    "floor",
    "Below",
  );
  const ceilingResults = sampleHorizontalBarrierPlane(
    room,
    room.ceiling.material,
    room.ceiling.thickness,
    room.roomHeight,
    "ceiling",
    "Above",
  );

  const maxLocation = findMaxAirKermaLocation(room);
  const allResults = [...results, ...floorResults, ...ceilingResults];
  const linacMazeAnalysis = room.linacMazeAnalysis?.enabled
    ? generateLinacMazeAnalysisReport(room.linacMazeAnalysis)
    : undefined;
  const overallPassed =
    (allResults.length === 0 || allResults.every((result) => result.passed)) &&
    (!linacMazeAnalysis ||
      (linacMazeAnalysis.mazeBarrierPassed &&
        linacMazeAnalysis.doorShieldingPassed));
  const totalUnshieldedAtWorst =
    maxLocation?.airKerma ||
    Math.max(...allResults.map((result) => result.unshieldedAirKerma), 0);

  return {
    timestamp: new Date(),
    room,
    results,
    overallPassed,
    maxAirKermaLocation: maxLocation
      ? { x: maxLocation.x, y: maxLocation.y }
      : null,
    totalUnshieldedAtWorst,
    floorResults,
    ceilingResults,
    linacMazeAnalysis,
  };
}

export function formatDose(dose        )         {
  if (dose === 0) return "0 mGy";
  if (dose < 0.001) {
    return `${(dose * 1000).toFixed(3)} μGy`;
  }
  if (dose < 0.1) {
    return `${dose.toFixed(4)} mGy`;
  }
  return `${dose.toFixed(2)} mGy`;
}

export function formatThickness(thickness        )         {
  if (thickness < 1) {
    return `${thickness.toFixed(2)} mm`;
  }
  return `${thickness.toFixed(1)} mm`;
}

export function getMaterialDisplayName(material              )         {
  const names                               = {
    lead: "Lead (Pb)",
    concrete: "Concrete",
    gypsum: "Gypsum Wallboard",
    steel: "Steel",
    glass: "Lead Glass",
    wood: "Wood",
  };

  return names[material] || material;
}
