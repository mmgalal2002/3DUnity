// Radiation shielding calculation types based on NCRP Report 147 methodology

                           
          
        
           
                 
           
             
                        
            
          
               
                 
          
             
                                                       
                                                                    

                                             
                              
                          
                          
                             
                         
                         
 

                                              
             
                
                      
 

                          
            
            
 

                                  
             
               
                            
            
            
            
                                                                                               
                                             
                                                       
                                                             
                                                           
                   
 

                                 
             
               
                    
                                            
                                      
                     
 

// Workload distribution types per NCRP 147
                                  
                          
                        
                                      
                      
                 
                
                
                
                  

                            
             
                      
               
            
            
                                                               
                                                             
                                                              
                                                                       
                                  
                                                                                                       
                                                                                                    
                                                                                                       
                        
                                                                     
                                                   
                                                                                     
                                             
                          
                                      
 

                              
             
               
            
            
                                                               
                                            
                                                           
 

                          
          
              
            
           
           
           

export const WALL_MATERIAL_DENSITY_G_PER_CM3                               = {
  lead: 11.34,
  concrete: 2.35,
  gypsum: 0.8,
  steel: 7.85,
  glass: 5.2,
  wood: 0.6,
};

                       
             
             
             
             
             
                          
                         
 

                       
                          
                                              
                                                                  
                
                         
                              
                                    
                                           
                                  
                                        
                           
                             
                                                                
                         
                                              
 

                                    
                          
                         
 

                                          
                   
                                  
                                  
                                 
                                            
                                 
                                                
                                        
                                              
 

                                          
                   
                       
                      
                    
                                   
                                      
                              
                                      
                                           
                                       
                                     
                                   
                                   
                                   
                                      
                                   
                                                  
                                  
                                      
                                      
                                      
                            
                             
                      
                                 
                                 
                                    
                                  
                               
                                          
                                             
                              
                             
                                      
 

                                          
                       
                                  
                               
                     
                                      
                                                 
                                         
                                             
    
                                     
                                      
                                  
                                    
                                
                                         
                                              
                                          
                             
                                      
                                     
                              
                             
                               
 

                                  
                        
                          
                      
                        
                              
                             
                               
                            
                                    
                              
                   
                                         
                                       
                                                                     
                                                       
                                     
                                                  
                  
                                                             
                                                                    
                                
 

                           
                  
             
                             
                         
                                                       
                                 
                                  
                                    
                                              
 

// NCRP Report 147 transmission fitting parameters
// B(x) = [1 + (β/α)] * exp(-α*γ*x) - (β/α) all raised to 1/γ
// From Table 4.1 and Appendix B of NCRP 147
                                     
                             
                            
                                     
 

// NCRP 147 Table B.2 - Primary transmission fitting parameters
export const PRIMARY_TRANSMISSION_PARAMS         
               
                                    
  = {
  lead: {
    "30kVp": { alpha: 33.7, beta: 0, gamma: 1 },
    "50kVp": { alpha: 13.0, beta: 0, gamma: 1 },
    "70kVp": { alpha: 5.85, beta: 0.217, gamma: 0.549 },
    "100kVp": { alpha: 2.32, beta: 0.074, gamma: 0.559 },
    "125kVp": { alpha: 1.51, beta: 0.052, gamma: 0.62 },
    "150kVp": { alpha: 1.09, beta: 0.039, gamma: 0.71 }, // Also used for leakage
  },
  concrete: {
    "30kVp": { alpha: 0.739, beta: 0, gamma: 1 },
    "50kVp": { alpha: 0.298, beta: 0, gamma: 1 },
    "70kVp": { alpha: 0.147, beta: 0.0035, gamma: 0.622 },
    "100kVp": { alpha: 0.0771, beta: 0.0022, gamma: 0.676 },
    "125kVp": { alpha: 0.0554, beta: 0.0026, gamma: 0.686 },
    "150kVp": { alpha: 0.0413, beta: 0.0029, gamma: 0.657 },
  },
  gypsum: {
    "30kVp": { alpha: 1.42, beta: 0, gamma: 1 },
    "50kVp": { alpha: 0.466, beta: 0, gamma: 1 },
    "70kVp": { alpha: 0.19, beta: 0.0053, gamma: 0.601 },
    "100kVp": { alpha: 0.0776, beta: 0.0018, gamma: 0.697 },
    "125kVp": { alpha: 0.0499, beta: 0.0019, gamma: 0.698 },
    "150kVp": { alpha: 0.034, beta: 0.0019, gamma: 0.668 },
  },
  steel: {
    "30kVp": { alpha: 4.79, beta: 0, gamma: 1 },
    "50kVp": { alpha: 1.93, beta: 0, gamma: 1 },
    "70kVp": { alpha: 1.08, beta: 0.048, gamma: 0.531 },
    "100kVp": { alpha: 0.461, beta: 0.022, gamma: 0.546 },
    "125kVp": { alpha: 0.316, beta: 0.017, gamma: 0.596 },
    "150kVp": { alpha: 0.232, beta: 0.014, gamma: 0.667 },
  },
  glass: {
    "30kVp": { alpha: 1.61, beta: 0, gamma: 1 },
    "50kVp": { alpha: 0.589, beta: 0, gamma: 1 },
    "70kVp": { alpha: 0.269, beta: 0.0074, gamma: 0.59 },
    "100kVp": { alpha: 0.118, beta: 0.0037, gamma: 0.651 },
    "125kVp": { alpha: 0.0815, beta: 0.004, gamma: 0.662 },
    "150kVp": { alpha: 0.0586, beta: 0.0041, gamma: 0.638 },
  },
  wood: {
    "30kVp": { alpha: 2.03, beta: 0, gamma: 1 },
    "50kVp": { alpha: 0.558, beta: 0, gamma: 1 },
    "70kVp": { alpha: 0.216, beta: 0.0073, gamma: 0.546 },
    "100kVp": { alpha: 0.0763, beta: 0.0012, gamma: 0.732 },
    "125kVp": { alpha: 0.0455, beta: 0.001, gamma: 0.743 },
    "150kVp": { alpha: 0.029, beta: 0.0008, gamma: 0.713 },
  },
};

// Secondary (scatter) transmission - uses similar fitting but with scatter parameters
export const SECONDARY_TRANSMISSION_PARAMS         
               
                                    
  = {
  lead: {
    fluoro: { alpha: 2.21, beta: 0.0365, gamma: 0.756 },
    rad_room: { alpha: 2.08, beta: 0.048, gamma: 0.696 },
    ct: { alpha: 1.64, beta: 0.0429, gamma: 0.755 },
    chest: { alpha: 2.5, beta: 0.063, gamma: 0.61 },
  },
  concrete: {
    fluoro: { alpha: 0.0658, beta: 0.0024, gamma: 0.68 },
    rad_room: { alpha: 0.068, beta: 0.0026, gamma: 0.66 },
    ct: { alpha: 0.0523, beta: 0.0024, gamma: 0.711 },
    chest: { alpha: 0.075, beta: 0.0034, gamma: 0.62 },
  },
  gypsum: {
    fluoro: { alpha: 0.0658, beta: 0.002, gamma: 0.73 },
    rad_room: { alpha: 0.068, beta: 0.0022, gamma: 0.71 },
    ct: { alpha: 0.044, beta: 0.0016, gamma: 0.76 },
    chest: { alpha: 0.075, beta: 0.0028, gamma: 0.68 },
  },
  steel: {
    fluoro: { alpha: 0.4, beta: 0.018, gamma: 0.58 },
    rad_room: { alpha: 0.41, beta: 0.02, gamma: 0.56 },
    ct: { alpha: 0.32, beta: 0.016, gamma: 0.62 },
    chest: { alpha: 0.45, beta: 0.024, gamma: 0.53 },
  },
  glass: {
    fluoro: { alpha: 0.1, beta: 0.0035, gamma: 0.66 },
    rad_room: { alpha: 0.105, beta: 0.0038, gamma: 0.64 },
    ct: { alpha: 0.082, beta: 0.0032, gamma: 0.69 },
    chest: { alpha: 0.115, beta: 0.0045, gamma: 0.6 },
  },
  wood: {
    fluoro: { alpha: 0.065, beta: 0.001, gamma: 0.75 },
    rad_room: { alpha: 0.068, beta: 0.0011, gamma: 0.73 },
    ct: { alpha: 0.052, beta: 0.0008, gamma: 0.78 },
    chest: { alpha: 0.072, beta: 0.0014, gamma: 0.7 },
  },
};

// NCRP 147 - Design goals (P) in mGy/week
export const DOSE_LIMITS = {
  controlled: 0.1, // mGy/week (equivalent to 5 mSv/year for occupational)
  uncontrolled: 0.02, // mGy/week (equivalent to 1 mSv/year for public)
};

export const EQUIPMENT_MECHANICAL_PROFILES         
                
                            
  = {
  xray: {
    defaultPlanRotation: 0,
    planRotationMin: 0,
    planRotationMax: 359,
    defaultGantryAngle: 0,
    gantryAngleMin: -90,
    gantryAngleMax: 90,
  },
  ct: {
    defaultPlanRotation: 0,
    planRotationMin: 0,
    planRotationMax: 359,
    defaultGantryAngle: 0,
    gantryAngleMin: -30,
    gantryAngleMax: 30,
  },
  linac: {
    defaultPlanRotation: 0,
    planRotationMin: 0,
    planRotationMax: 359,
    defaultGantryAngle: 0,
    gantryAngleMin: -185,
    gantryAngleMax: 185,
  },
  fluoroscopy: {
    defaultPlanRotation: 0,
    planRotationMin: 0,
    planRotationMax: 359,
    defaultGantryAngle: 35,
    gantryAngleMin: -45,
    gantryAngleMax: 110,
  },
  mammo: {
    defaultPlanRotation: 0,
    planRotationMin: 0,
    planRotationMax: 359,
    defaultGantryAngle: 0,
    gantryAngleMin: -25,
    gantryAngleMax: 25,
  },
  dental: {
    defaultPlanRotation: 0,
    planRotationMin: 0,
    planRotationMax: 359,
    defaultGantryAngle: 0,
    gantryAngleMin: -15,
    gantryAngleMax: 15,
  },
};

export function normalizeAngleDegrees(angle        )         {
  const normalized = angle % 360;
  return normalized < 0 ? normalized + 360 : normalized;
}

export function clampEquipmentPlanRotation(
  type               ,
  angle        ,
)         {
  const profile = EQUIPMENT_MECHANICAL_PROFILES[type];
  const normalized = normalizeAngleDegrees(angle);

  if (profile.planRotationMin === 0 && profile.planRotationMax === 359) {
    return normalized;
  }

  return Math.min(
    Math.max(normalized, profile.planRotationMin),
    profile.planRotationMax,
  );
}

export function clampEquipmentGantryAngle(
  type               ,
  angle        ,
)         {
  const profile = EQUIPMENT_MECHANICAL_PROFILES[type];
  return Math.min(
    Math.max(angle, profile.gantryAngleMin),
    profile.gantryAngleMax,
  );
}

export function getEquipmentPlanRotation(
  equipment                                                 ,
)         {
  return clampEquipmentPlanRotation(
    equipment.type,
    equipment.planRotationDegrees ??
      EQUIPMENT_MECHANICAL_PROFILES[equipment.type].defaultPlanRotation,
  );
}

export function getEquipmentGantryAngle(
  equipment                                                ,
)         {
  return clampEquipmentGantryAngle(
    equipment.type,
    equipment.gantryAngleDegrees ??
      EQUIPMENT_MECHANICAL_PROFILES[equipment.type].defaultGantryAngle,
  );
}

function offsetPoint(
  x        ,
  y        ,
  angleDegrees        ,
  forwardDistance        ,
  lateralDistance         = 0,
)                           {
  const radians = (angleDegrees * Math.PI) / 180;
  const forwardX = Math.cos(radians);
  const forwardY = Math.sin(radians);
  const lateralX = -forwardY;
  const lateralY = forwardX;

  return {
    x: x + forwardX * forwardDistance + lateralX * lateralDistance,
    y: y + forwardY * forwardDistance + lateralY * lateralDistance,
  };
}

function polarPoint(
  centerX        ,
  centerY        ,
  radius        ,
  angleDegrees        ,
)                           {
  const radians = (angleDegrees * Math.PI) / 180;
  return {
    x: centerX + Math.cos(radians) * radius,
    y: centerY + Math.sin(radians) * radius,
  };
}

function angleBetweenPoints(
  fromX        ,
  fromY        ,
  toX        ,
  toY        ,
)         {
  return normalizeAngleDegrees(
    (Math.atan2(toY - fromY, toX - fromX) * 180) / Math.PI,
  );
}

function getEffectiveGantryElevation(
  type               ,
  presetId        ,
  gantryAngle        ,
)         {
  switch (type) {
    case "mammo":
      return -90 + gantryAngle;
    case "fluoroscopy":
      return presetId === "fluoro-over-table"
        ? -Math.abs(gantryAngle)
        : Math.abs(gantryAngle);
    default:
      return gantryAngle;
  }
}

// Equipment default configurations based on NCRP 147 typical values
export const EQUIPMENT_DEFAULTS         
                
                                            
  = {
  xray: {
    type: "xray",
    z: 1.2,
    planRotationDegrees: 0,
    gantryAngleDegrees: 0,
    workload: 400, // mA-min/week
    kVp: 100,
    K1: 1.5, // mGy at 1m per patient
    nPatients: 100,
    useFactor: 0.25, // 1/4 for walls, 1 for floors
    workloadDistribution: "rad_room_all_other",
    sourcePresetId: "general-rad",
    sourceComponents: [],
  },
  ct: {
    type: "ct",
    z: 1.0,
    planRotationDegrees: 0,
    gantryAngleDegrees: 0,
    workload: 1000,
    kVp: 140,
    K1: 30, // CT has much higher output
    nPatients: 50,
    useFactor: 1.0, // CT rotates, so all directions
    workloadDistribution: "ct_scanner",
    sourcePresetId: "ct-axial",
    sourceComponents: [],
  },
  linac: {
    type: "linac",
    z: 1.3,
    planRotationDegrees: 0,
    gantryAngleDegrees: 0,
    workload: 2000,
    kVp: 6000, // MV
    linacFieldSizeCm: 40,
    linacScatterAngleDegrees: 90,
    linacLeakageFactor: 1,
    K1: 100,
    nPatients: 30,
    useFactor: 0.25,
    workloadDistribution: "rad_room_all_other",
    sourcePresetId: "linac-cardinal",
    sourceComponents: [],
  },
  fluoroscopy: {
    type: "fluoroscopy",
    z: 1.2,
    planRotationDegrees: 0,
    gantryAngleDegrees: 35,
    workload: 600,
    kVp: 80,
    K1: 3.0,
    nPatients: 80,
    useFactor: 1.0, // Under-table tube - scatter in all directions
    workloadDistribution: "fluoro_tube",
    sourcePresetId: "fluoro-under-table",
    sourceComponents: [],
  },
  mammo: {
    type: "mammo",
    z: 1.4,
    planRotationDegrees: 0,
    gantryAngleDegrees: 0,
    workload: 200,
    kVp: 30,
    K1: 0.2, // Very low energy
    nPatients: 150,
    useFactor: 1.0,
    workloadDistribution: "mammo_unit",
    sourcePresetId: "mammo-upright",
    sourceComponents: [],
  },
  dental: {
    type: "dental",
    z: 1.25,
    planRotationDegrees: 0,
    gantryAngleDegrees: 0,
    workload: 120,
    kVp: 70,
    K1: 0.35,
    nPatients: 60,
    useFactor: 1.0,
    workloadDistribution: "dental_unit",
    sourcePresetId: "dental-pan",
    sourceComponents: [],
  },
};

export const SOURCE_COMPONENT_PRESETS         
                
                               
  = {
  xray: [
    {
      id: "general-rad",
      label: "General Rad",
      description: "Single primary beam, patient scatter, tube leakage.",
    },
    {
      id: "chest-bucky",
      label: "Chest Bucky",
      description: "Narrow chest bucky beam with nearby scatter origin.",
    },
    {
      id: "cross-table",
      label: "Cross-Table",
      description: "Lateral room beam with shifted scatter center.",
    },
  ],
  ct: [
    {
      id: "ct-axial",
      label: "CT Axial",
      description:
        "Four-cardinal gantry primary components plus central scatter and leakage.",
    },
    {
      id: "ct-wide-rotation",
      label: "CT Wide Rotation",
      description:
        "Eight-direction rotation for highly distributed primary workload.",
    },
  ],
  linac: [
    {
      id: "linac-cardinal",
      label: "LINAC Cardinal",
      description:
        "Cardinal treatment fields with isocenter scatter and head leakage.",
    },
    {
      id: "linac-oblique",
      label: "LINAC Oblique",
      description: "Oblique field arrangement for vault corner loading.",
    },
  ],
  fluoroscopy: [
    {
      id: "fluoro-under-table",
      label: "Under-Table Tube",
      description:
        "Upward-angled primary beam with patient scatter above the couch.",
    },
    {
      id: "fluoro-over-table",
      label: "Over-Table Tube",
      description: "Downward-angled beam with scatter near the tabletop.",
    },
  ],
  mammo: [
    {
      id: "mammo-upright",
      label: "Upright Mammo",
      description:
        "Tight downward primary beam with breast scatter and tube leakage.",
    },
    {
      id: "mammo-stereo",
      label: "Stereo / Angled",
      description: "Dual oblique mammography beam arrangement.",
    },
  ],
  dental: [
    {
      id: "dental-pan",
      label: "Panoramic / CBCT",
      description:
        "Rotating panoramic arm with central patient scatter and tube leakage.",
    },
    {
      id: "dental-intraoral",
      label: "Intraoral Tube Head",
      description:
        "Wall-mounted or chairside dental X-ray tube with narrow beam.",
    },
  ],
};

export function getDefaultSourcePresetId(type               )         {
  return SOURCE_COMPONENT_PRESETS[type][0]?.id ?? "general-rad";
}

function createComponentId(type                     , suffix        )         {
  return `src-${type}-${suffix}-${Date.now()}`;
}

function makePrimaryComponent(
  suffix        ,
  name        ,
  x        ,
  y        ,
  z        ,
  relativeOutput        ,
  workloadDistribution                      ,
  directionDegrees        ,
  elevationDegrees        ,
  beamWidthDegrees        ,
)                  {
  return {
    id: createComponentId("primary", suffix),
    name,
    type: "primary",
    x,
    y,
    z,
    relativeOutput,
    workloadDistribution,
    directionDegrees,
    elevationDegrees,
    beamWidthDegrees,
    enabled: true,
  };
}

function makeScatterComponent(
  suffix        ,
  name        ,
  x        ,
  y        ,
  z        ,
  relativeOutput        ,
  workloadDistribution                      ,
  directionDegrees         = 0,
  elevationDegrees         = 0,
  beamWidthDegrees         = 180,
)                  {
  return {
    id: createComponentId("scatter", suffix),
    name,
    type: "scatter",
    x,
    y,
    z,
    relativeOutput,
    workloadDistribution,
    directionDegrees,
    elevationDegrees,
    beamWidthDegrees,
    enabled: true,
  };
}

function makeLeakageComponent(
  suffix        ,
  name        ,
  x        ,
  y        ,
  z        ,
  relativeOutput        ,
  workloadDistribution                      ,
)                  {
  return {
    id: createComponentId("leakage", suffix),
    name,
    type: "leakage",
    x,
    y,
    z,
    relativeOutput,
    workloadDistribution,
    directionDegrees: 0,
    elevationDegrees: 0,
    beamWidthDegrees: 360,
    enabled: true,
  };
}

export function createSourceComponentsForPreset(
  type               ,
  presetId        ,
  x        ,
  y        ,
  z        ,
  workloadDistribution                      ,
  useFactor        ,
  planRotationDegrees         ,
  gantryAngleDegrees         ,
)                    {
  const primaryOutput = Math.max(useFactor, 0.05);
  const mechanicalProfile = EQUIPMENT_MECHANICAL_PROFILES[type];
  const planRotation = clampEquipmentPlanRotation(
    type,
    planRotationDegrees ?? mechanicalProfile.defaultPlanRotation,
  );
  const gantryAngle = clampEquipmentGantryAngle(
    type,
    gantryAngleDegrees ?? mechanicalProfile.defaultGantryAngle,
  );
  const effectiveElevation = getEffectiveGantryElevation(
    type,
    presetId,
    gantryAngle,
  );

  switch (type) {
    case "xray":
      switch (presetId) {
        case "chest-bucky": {
          const scatterPoint = offsetPoint(x, y, planRotation, 0.35);
          return [
            makePrimaryComponent(
              "primary",
              "Primary Beam",
              x,
              y,
              z,
              Math.max(primaryOutput, 0.25),
              "rad_room_chest_bucky",
              planRotation,
              effectiveElevation,
              25,
            ),
            makeScatterComponent(
              "scatter",
              "Patient / Scatter",
              scatterPoint.x,
              scatterPoint.y,
              z,
              0.12,
              "rad_room_chest_bucky",
              planRotation,
              0,
              180,
            ),
            makeLeakageComponent(
              "leakage",
              "Tube Leakage",
              x,
              y,
              z,
              0.001,
              workloadDistribution,
            ),
          ];
        }
        case "cross-table": {
          const primaryDirection = normalizeAngleDegrees(planRotation + 90);
          const scatterPoint = offsetPoint(x, y, primaryDirection, 0.65);
          return [
            makePrimaryComponent(
              "primary",
              "Primary Beam",
              x,
              y,
              z,
              primaryOutput,
              workloadDistribution,
              primaryDirection,
              effectiveElevation,
              50,
            ),
            makeScatterComponent(
              "scatter",
              "Patient / Scatter",
              scatterPoint.x,
              scatterPoint.y,
              z,
              0.18,
              workloadDistribution,
              primaryDirection,
              0,
              220,
            ),
            makeLeakageComponent(
              "leakage",
              "Tube Leakage",
              x,
              y,
              z,
              0.001,
              workloadDistribution,
            ),
          ];
        }
        default: {
          const scatterPoint = offsetPoint(x, y, planRotation, 0.9);
          return [
            makePrimaryComponent(
              "primary",
              "Primary Beam",
              x,
              y,
              z,
              primaryOutput,
              workloadDistribution,
              planRotation,
              effectiveElevation,
              40,
            ),
            makeScatterComponent(
              "scatter",
              "Patient / Scatter",
              scatterPoint.x,
              scatterPoint.y,
              z,
              0.15,
              workloadDistribution,
              planRotation,
              0,
              180,
            ),
            makeLeakageComponent(
              "leakage",
              "Tube Leakage",
              x,
              y,
              z,
              0.001,
              workloadDistribution,
            ),
          ];
        }
      }
    case "ct": {
      const sourceRadius = 0.95;
      const sourceAngles =
        presetId === "ct-wide-rotation"
          ? [0, 45, 90, 135, 180, 225, 270, 315]
          : [0, 90, 180, 270];
      const primaryShare = presetId === "ct-wide-rotation" ? 0.125 : 0.25;
      const beamWidth = presetId === "ct-wide-rotation" ? 90 : 120;
      const rotatedAngles = sourceAngles.map((angle) =>
        normalizeAngleDegrees(planRotation + angle),
      );
      const primaryComponents = rotatedAngles.map((sourceAngle, index) => {
        const sourcePoint = polarPoint(x, y, sourceRadius, sourceAngle);
        return makePrimaryComponent(
          `primary-${index + 1}`,
          `Primary ${sourceAngle.toFixed(0)}°`,
          sourcePoint.x,
          sourcePoint.y,
          z,
          primaryShare,
          "ct_scanner",
          angleBetweenPoints(sourcePoint.x, sourcePoint.y, x, y),
          effectiveElevation,
          beamWidth,
        );
      });
      const leakagePoint = polarPoint(x, y, sourceRadius, planRotation);
      return [
        ...primaryComponents,
        makeScatterComponent(
          "scatter",
          "Patient / Scatter",
          x,
          y,
          z,
          presetId === "ct-wide-rotation" ? 0.2 : 0.18,
          "ct_scanner",
          0,
          0,
          360,
        ),
        makeLeakageComponent(
          "leakage",
          "Tube Leakage",
          leakagePoint.x,
          leakagePoint.y,
          z,
          0.001,
          "ct_scanner",
        ),
      ];
    }
    case "linac": {
      const fieldOffsets =
        presetId === "linac-oblique" ? [45, 135, 225, 315] : [0, 90, 180, 270];
      const sourcePoint = offsetPoint(x, y, planRotation, 0.35);
      return [
        ...fieldOffsets.map((offset, index) =>
          makePrimaryComponent(
            `primary-${index + 1}`,
            `Field ${normalizeAngleDegrees(planRotation + offset).toFixed(0)}°`,
            sourcePoint.x,
            sourcePoint.y,
            z,
            0.25,
            workloadDistribution,
            normalizeAngleDegrees(planRotation + offset),
            effectiveElevation,
            25,
          ),
        ),
        makeScatterComponent(
          "scatter",
          "Isocenter Scatter",
          x,
          y,
          z,
          0.1,
          workloadDistribution,
          planRotation,
          0,
          360,
        ),
        makeLeakageComponent(
          "leakage",
          "Head Leakage",
          sourcePoint.x,
          sourcePoint.y,
          z,
          0.001,
          workloadDistribution,
        ),
      ];
    }
    case "fluoroscopy":
      if (presetId === "fluoro-over-table") {
        const scatterPoint = offsetPoint(x, y, planRotation, 0.4);
        return [
          makePrimaryComponent(
            "primary",
            "Primary Beam",
            x,
            y,
            z,
            primaryOutput,
            "fluoro_tube",
            planRotation,
            effectiveElevation,
            70,
          ),
          makeScatterComponent(
            "scatter",
            "Patient / Scatter",
            scatterPoint.x,
            scatterPoint.y,
            z - 0.2,
            0.2,
            "fluoro_tube",
            planRotation,
            -10,
            240,
          ),
          makeLeakageComponent(
            "leakage",
            "Tube Leakage",
            x,
            y,
            z,
            0.001,
            "fluoro_tube",
          ),
        ];
      }

      {
        const scatterPoint = offsetPoint(x, y, planRotation, 0.4);
        return [
          makePrimaryComponent(
            "primary",
            "Primary Beam",
            x,
            y,
            z,
            primaryOutput,
            "fluoro_tube",
            planRotation,
            effectiveElevation,
            70,
          ),
          makeScatterComponent(
            "scatter",
            "Patient / Scatter",
            scatterPoint.x,
            scatterPoint.y,
            z + 0.45,
            0.2,
            "fluoro_tube",
            planRotation,
            10,
            240,
          ),
          makeLeakageComponent(
            "leakage",
            "Tube Leakage",
            x,
            y,
            z,
            0.001,
            "fluoro_tube",
          ),
        ];
      }
    case "mammo":
      if (presetId === "mammo-stereo") {
        return [
          makePrimaryComponent(
            "primary-left",
            "Primary -7.5°",
            x,
            y,
            z,
            0.5,
            "mammo_unit",
            normalizeAngleDegrees(planRotation - 7.5),
            effectiveElevation,
            22,
          ),
          makePrimaryComponent(
            "primary-right",
            "Primary +7.5°",
            x,
            y,
            z,
            0.5,
            "mammo_unit",
            normalizeAngleDegrees(planRotation + 7.5),
            effectiveElevation,
            22,
          ),
          makeScatterComponent(
            "scatter",
            "Breast Scatter",
            offsetPoint(x, y, planRotation, 0.2).x,
            offsetPoint(x, y, planRotation, 0.2).y,
            z - 0.25,
            0.1,
            "mammo_unit",
            planRotation,
            -45,
            180,
          ),
          makeLeakageComponent(
            "leakage",
            "Tube Leakage",
            x,
            y,
            z,
            0.001,
            "mammo_unit",
          ),
        ];
      }

      return [
        makePrimaryComponent(
          "primary",
          "Primary Beam",
          x,
          y,
          z,
          1,
          "mammo_unit",
          planRotation,
          effectiveElevation,
          20,
        ),
        makeScatterComponent(
          "scatter",
          "Breast Scatter",
          offsetPoint(x, y, planRotation, 0.2).x,
          offsetPoint(x, y, planRotation, 0.2).y,
          z - 0.25,
          0.08,
          "mammo_unit",
          planRotation,
          -45,
          180,
        ),
        makeLeakageComponent(
          "leakage",
          "Tube Leakage",
          x,
          y,
          z,
          0.001,
          "mammo_unit",
        ),
      ];
    case "dental":
      if (presetId === "dental-intraoral") {
        const scatterPoint = offsetPoint(x, y, planRotation, 0.25);
        return [
          makePrimaryComponent(
            "primary",
            "Primary Beam",
            x,
            y,
            z,
            primaryOutput,
            "dental_unit",
            planRotation,
            effectiveElevation,
            18,
          ),
          makeScatterComponent(
            "scatter",
            "Patient / Scatter",
            scatterPoint.x,
            scatterPoint.y,
            z,
            0.06,
            "dental_unit",
            planRotation,
            0,
            140,
          ),
          makeLeakageComponent(
            "leakage",
            "Tube Leakage",
            x,
            y,
            z,
            0.001,
            "dental_unit",
          ),
        ];
      }

      {
        const sourcePoint = polarPoint(x, y, 0.8, planRotation);
        return [
          makePrimaryComponent(
            "primary",
            "Primary Beam",
            sourcePoint.x,
            sourcePoint.y,
            z,
            primaryOutput,
            "dental_unit",
            angleBetweenPoints(sourcePoint.x, sourcePoint.y, x, y),
            effectiveElevation,
            20,
          ),
          makeScatterComponent(
            "scatter",
            "Patient / Scatter",
            x,
            y,
            z,
            0.08,
            "dental_unit",
            planRotation,
            0,
            220,
          ),
          makeLeakageComponent(
            "leakage",
            "Tube Leakage",
            sourcePoint.x,
            sourcePoint.y,
            z,
            0.001,
            "dental_unit",
          ),
        ];
      }
  }
}

export function createDefaultSourceComponents(
  type               ,
  x        ,
  y        ,
  z        ,
  workloadDistribution                      ,
  useFactor        ,
  planRotationDegrees         ,
  gantryAngleDegrees         ,
)                    {
  return createSourceComponentsForPreset(
    type,
    getDefaultSourcePresetId(type),
    x,
    y,
    z,
    workloadDistribution,
    useFactor,
    planRotationDegrees,
    gantryAngleDegrees,
  );
}

export function createDefaultLinacMazeAnalysisConfig()                          {
  return {
    enabled: false,
    scenarioName: "NCRP 151 Chapter 7 Maze / Door",
    fieldSizeCm: 40,
    useFactor: 0.25,
    doorDesignGoalMsvPerWeek: 0.1,
    roomScatterCorrectionFactor: 2.64,
    primaryWallFraction: 0.34,
    wallScatterProjectionAreaM2: 2.82,
    wallScatterReflectionCoefficient: 8e-3,
    mazeEntranceProjectionAreaM2: 8.4,
    wallScatterSourceDistanceM: 4.2,
    wallScatterMazeDistanceM: 5.9,
    wallScatterDoorDistanceM: 6.8,
    headLeakageScatterAreaM2: 11.8,
    headLeakageScatterDistanceM: 7.9,
    headLeakageDoorDistanceM: 9.9,
    patientScatterWallReflectionCoefficient: 2.2e-2,
    patientScatterDistanceM: 1,
    patientScatterMazeDistanceM: 7.3,
    patientScatterDoorDistanceM: 9.9,
    transmittedLeakageDistanceM: 7.3,
    roomSurfaceAreaM2: 236,
    innerMazeDistanceM: 6.4,
    mazeLengthM: 8.5,
    innerMazeOpeningAreaM2: 9.2,
    mazeCrossSectionAreaM2: 8.4,
    mazeBarrierMaterial: "concrete",
    mazeBarrierAngleDegrees: 40,
    mazeBarrierDistanceM: 7.7,
    installedMazeBarrierThicknessMm: 958,
    minimumMazeBarrierSlantThicknessMm: 1250,
    installedDoorLeadMm: 7,
    installedDoorBpeMm: 54,
    energies: [
      {
        energyMV: 18,
        primaryWeeklyWorkloadGy: 450,
        leakageWeeklyWorkloadGy: 450,
        wallScatterCoefficient: 1.6e-3,
        headLeakageWallScatterCoefficient: 4.5e-3,
        patientScatterFraction: 0.864e-3,
        transmittedLeakageBarrierTransmission: 2.41e-4,
        neutronSourceStrengthPerGray: 1.22e12,
        neutronDoseEquivalentPerGrayAtDoor: 1.7e-6,
      },
      {
        energyMV: 6,
        primaryWeeklyWorkloadGy: 225,
        leakageWeeklyWorkloadGy: 225,
        wallScatterCoefficient: 2.7e-3,
        headLeakageWallScatterCoefficient: 6.4e-3,
        patientScatterFraction: 1.39e-3,
        transmittedLeakageBarrierTransmission: 3.18e-5,
      },
    ],
  };
}

// Map kVp to transmission parameter key
export function getKvpKey(kVp        )         {
  if (kVp <= 30) return "30kVp";
  if (kVp <= 50) return "50kVp";
  if (kVp <= 70) return "70kVp";
  if (kVp <= 100) return "100kVp";
  if (kVp <= 125) return "125kVp";
  return "150kVp";
}

// Map workload distribution to scatter category
export function getScatterCategory(dist                      )         {
  switch (dist) {
    case "fluoro_tube":
    case "rad_tube_r_and_f":
      return "fluoro";
    case "ct_scanner":
      return "ct";
    case "chest_room":
    case "rad_room_chest_bucky":
      return "chest";
    default:
      return "rad_room";
  }
}
