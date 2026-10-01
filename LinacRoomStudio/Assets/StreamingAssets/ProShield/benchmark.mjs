import {
  calculateLinacSecondaryBarrierBenchmark,
  calculateLinacMazeBarrierBenchmark,
  calculateLinacMazeCaptureGammaDose,
  calculateLinacMazeDoorPhotonDose,
  calculateLinacMazeDoorShielding,
  calculateLinacMazeNeutronDoorDose,
                                           
} from "./radiation-calculations.mjs";

                                         
               
                                                
                              
                       
                
 

                               
               
                 
                   
                    
                                 
                
 

const NCRP_DESIGN_GOAL_MGY_PER_WEEK = 0.02;
const OCCUPANCY_UNCONTROLLED = 1 / 40;
const NCRP_MAZE_DOOR_DESIGN_GOAL_MSV_PER_WEEK = 0.1;
const MICROSIEVERT = 1e-6;
const LOCATION_H_ANGLE_DEGREES = 30;
const MAZE_BARRIER_ANGLE_DEGREES = 40;
const LOCATION_H_COS = Math.cos((LOCATION_H_ANGLE_DEGREES * Math.PI) / 180);
const TRANSMITTED_LEAKAGE_6MV =
  (0.08 * MICROSIEVERT * Math.pow(7.3, 2)) / (225 * 0.25 * 1e-3);

function formatScalar(
  value        ,
  unit                             ,
)         {
  if (unit === "cm") {
    return `${(value / 10).toFixed(1)} cm`;
  }

  if (unit === "uSv/week") {
    return `${(value / MICROSIEVERT).toFixed(1)} uSv/week`;
  }

  return `${value.toFixed(1)} mm`;
}

function evaluateBarrierCase(testCase                               )   
                            
                       
                  
  {
  const result = calculateLinacSecondaryBarrierBenchmark(testCase.scenario);
  const actualThicknessMm = result.requiredThickness;
  const differenceMm = actualThicknessMm - testCase.expectedThicknessMm;

  return {
    actualThicknessMm,
    differenceMm,
    passed: Math.abs(differenceMm) <= (testCase.toleranceMm ?? 2),
  };
}

function evaluateScalarCase(testCase                     ) {
  const difference = testCase.actual - testCase.expected;

  return {
    difference,
    passed: Math.abs(difference) <= testCase.tolerance,
  };
}

const baseComponents = [
  {
    sourceType: "scatter"         ,
    energyMV: 18,
    weeklyWorkload: 45000,
    distanceMeters: 7.2,
    useFactor: 0.25,
    fieldSizeCm: 40,
    scatterAngleDegrees: 30,
  },
  {
    sourceType: "scatter"         ,
    energyMV: 6,
    weeklyWorkload: 22500,
    distanceMeters: 7.2,
    useFactor: 0.25,
    fieldSizeCm: 40,
    scatterAngleDegrees: 30,
  },
  {
    sourceType: "leakage"         ,
    energyMV: 18,
    weeklyWorkload: 45000,
    distanceMeters: 7.2,
    leakageFactor: 1,
  },
  {
    sourceType: "leakage"         ,
    energyMV: 6,
    weeklyWorkload: 22500,
    distanceMeters: 7.2,
    leakageFactor: 1,
  },
];

const locationHComponents = [
  {
    sourceType: "scatter"         ,
    energyMV: 18,
    weeklyWorkload: 45000,
    distanceMeters: 3.9,
    useFactor: 0.25,
    fieldSizeCm: 40,
    scatterAngleDegrees: 30,
    angleOfIncidenceDegrees: LOCATION_H_ANGLE_DEGREES,
  },
  {
    sourceType: "scatter"         ,
    energyMV: 6,
    weeklyWorkload: 22500,
    distanceMeters: 3.9,
    useFactor: 0.25,
    fieldSizeCm: 40,
    scatterAngleDegrees: 30,
    angleOfIncidenceDegrees: LOCATION_H_ANGLE_DEGREES,
  },
  {
    sourceType: "leakage"         ,
    energyMV: 18,
    weeklyWorkload: 45000,
    distanceMeters: 3.9,
    leakageFactor: 1,
    angleOfIncidenceDegrees: LOCATION_H_ANGLE_DEGREES,
  },
  {
    sourceType: "leakage"         ,
    energyMV: 6,
    weeklyWorkload: 22500,
    distanceMeters: 3.9,
    leakageFactor: 1,
    angleOfIncidenceDegrees: LOCATION_H_ANGLE_DEGREES,
  },
];

const secondaryBarrierCases                                  = [
  {
    name: "Location A conventional",
    scenario: {
      material: "concrete",
      designGoal: NCRP_DESIGN_GOAL_MGY_PER_WEEK,
      occupancyFactor: OCCUPANCY_UNCONTROLLED,
      components: baseComponents,
    },
    expectedThicknessMm: 664,
    notes: "Published wall thickness: 66.4 cm",
  },
  {
    name: "Location A IMRT",
    scenario: {
      material: "concrete",
      designGoal: NCRP_DESIGN_GOAL_MGY_PER_WEEK,
      occupancyFactor: OCCUPANCY_UNCONTROLLED,
      components: baseComponents.map((component) =>
        component.sourceType === "leakage"
          ? {
              ...component,
              leakageFactor: component.energyMV === 18 ? 2.6 : 4.2,
            }
          : component,
      ),
    },
    expectedThicknessMm: 730,
    notes: "Published wall thickness: 73.0 cm",
  },
  {
    name: "Location H conventional",
    scenario: {
      material: "concrete",
      designGoal: NCRP_DESIGN_GOAL_MGY_PER_WEEK,
      occupancyFactor: OCCUPANCY_UNCONTROLLED,
      combinationRule: "oblique",
      components: locationHComponents,
    },
    expectedThicknessMm: 730 * LOCATION_H_COS,
    toleranceMm: 5,
    notes: "Published ceiling thickness: 73 cm slant, 63.2 cm normal",
  },
  {
    name: "Location H IMRT",
    scenario: {
      material: "concrete",
      designGoal: NCRP_DESIGN_GOAL_MGY_PER_WEEK,
      occupancyFactor: OCCUPANCY_UNCONTROLLED,
      combinationRule: "oblique",
      components: locationHComponents.map((component) =>
        component.sourceType === "leakage"
          ? {
              ...component,
              leakageFactor: component.energyMV === 18 ? 2.6 : 4.2,
            }
          : component,
      ),
    },
    expectedThicknessMm: 790,
    notes: "Published final ceiling thickness after verification: 79 cm",
  },
];

const secondaryBarrierResults = secondaryBarrierCases.map((testCase) => {
  const evaluation = evaluateBarrierCase(testCase);

  return {
    Case: testCase.name,
    Expected: formatScalar(testCase.expectedThicknessMm, "cm"),
    Actual: formatScalar(evaluation.actualThicknessMm, "cm"),
    Delta: `${(evaluation.differenceMm / 10).toFixed(1)} cm`,
    Status: evaluation.passed ? "PASS" : "FAIL",
    Notes: testCase.notes,
  };
});

const mazePhotonConventional = calculateLinacMazeDoorPhotonDose({
  energies: [
    {
      energyMV: 18,
      weeklyWorkload: 450,
      wallScatterCoefficient: 1.6e-3,
      headLeakageWallScatterCoefficient: 4.5e-3,
      patientScatterFraction: 0.864e-3,
      transmittedLeakageBarrierTransmission: 2.41e-4,
    },
    {
      energyMV: 6,
      weeklyWorkload: 225,
      wallScatterCoefficient: 2.7e-3,
      headLeakageWallScatterCoefficient: 6.4e-3,
      patientScatterFraction: 1.39e-3,
      transmittedLeakageBarrierTransmission: TRANSMITTED_LEAKAGE_6MV,
    },
  ],
  useFactor: 0.25,
  fieldSizeCm: 40,
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
});

const mazePhotonImrt = calculateLinacMazeDoorPhotonDose({
  energies: [
    {
      energyMV: 18,
      weeklyWorkload: 450,
      headLeakageWeeklyWorkload: 1170,
      transmittedLeakageWeeklyWorkload: 1170,
      wallScatterCoefficient: 1.6e-3,
      headLeakageWallScatterCoefficient: 4.5e-3,
      patientScatterFraction: 0.864e-3,
      transmittedLeakageBarrierTransmission: 2.41e-4,
    },
    {
      energyMV: 6,
      weeklyWorkload: 225,
      headLeakageWeeklyWorkload: 945,
      transmittedLeakageWeeklyWorkload: 945,
      wallScatterCoefficient: 2.7e-3,
      headLeakageWallScatterCoefficient: 6.4e-3,
      patientScatterFraction: 1.39e-3,
      transmittedLeakageBarrierTransmission: TRANSMITTED_LEAKAGE_6MV,
    },
  ],
  useFactor: 0.25,
  fieldSizeCm: 40,
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
});

const mazeCaptureGammaConventional = calculateLinacMazeCaptureGammaDose({
  leakageWorkload: 450,
  neutronSourceStrengthPerGray: 1.22e12,
  innerMazeDistanceM: 6.4,
  mazeLengthM: 8.5,
  roomSurfaceAreaM2: 236,
});

const mazeCaptureGammaImrt = calculateLinacMazeCaptureGammaDose({
  leakageWorkload: 1170,
  neutronSourceStrengthPerGray: 1.22e12,
  innerMazeDistanceM: 6.4,
  mazeLengthM: 8.5,
  roomSurfaceAreaM2: 236,
});

const mazeNeutronKerseyConventional = calculateLinacMazeNeutronDoorDose({
  method: "kersey",
  leakageWorkload: 450,
  neutronDoseEquivalentPerGrayAtDoor: 1.7e-6,
});

const mazeNeutronKerseyImrt = calculateLinacMazeNeutronDoorDose({
  method: "kersey",
  leakageWorkload: 1170,
  neutronDoseEquivalentPerGrayAtDoor: 1.7e-6,
});

const mazeNeutronWu = calculateLinacMazeNeutronDoorDose({
  method: "wu-mcginley",
  leakageWorkload: 450,
  neutronFluenceAtInnerMazePoint:
    mazeCaptureGammaConventional.innerMazeNeutronFluence,
  innerMazeOpeningAreaM2: 9.2,
  mazeCrossSectionAreaM2: 8.4,
  mazeLengthM: 8.5,
});

const mazeBarrier = calculateLinacMazeBarrierBenchmark({
  material: "concrete",
  designGoal: NCRP_MAZE_DOOR_DESIGN_GOAL_MSV_PER_WEEK,
  occupancyFactor: 1,
  angleOfIncidenceDegrees: MAZE_BARRIER_ANGLE_DEGREES,
  minimumSlantThickness: 1250,
  components: [
    { energyMV: 18, weeklyWorkload: 117000, distanceMeters: 7.7 },
    { energyMV: 6, weeklyWorkload: 94500, distanceMeters: 7.7 },
  ],
});

const doorShieldingConventional = calculateLinacMazeDoorShielding({
  designGoal: NCRP_MAZE_DOOR_DESIGN_GOAL_MSV_PER_WEEK * 1000 * MICROSIEVERT,
  neutronDoseEquivalent:
    mazeNeutronKerseyConventional.weeklyNeutronDoseEquivalent,
  captureGammaDoseEquivalent:
    mazeCaptureGammaConventional.weeklyCaptureGammaDoseEquivalent,
});

const doorShieldingImrt = calculateLinacMazeDoorShielding({
  designGoal: NCRP_MAZE_DOOR_DESIGN_GOAL_MSV_PER_WEEK * 1000 * MICROSIEVERT,
  neutronDoseEquivalent: mazeNeutronKerseyImrt.weeklyNeutronDoseEquivalent,
  captureGammaDoseEquivalent:
    mazeCaptureGammaImrt.weeklyCaptureGammaDoseEquivalent,
});

const scalarCases                        = [
  {
    name: "Maze door photons conventional",
    actual: mazePhotonConventional.totalPhotonDoseEquivalent,
    expected: 100 * MICROSIEVERT,
    tolerance: 1.5 * MICROSIEVERT,
    unit: "uSv/week",
    notes: "Published Htot = 100 uSv/week",
  },
  {
    name: "Maze door photons IMRT",
    actual: mazePhotonImrt.totalPhotonDoseEquivalent,
    expected: 113 * MICROSIEVERT,
    tolerance: 1.5 * MICROSIEVERT,
    unit: "uSv/week",
    notes: "Published Htot = 113 uSv/week",
  },
  {
    name: "Capture gamma conventional",
    actual: mazeCaptureGammaConventional.weeklyCaptureGammaDoseEquivalent,
    expected: 65.3 * MICROSIEVERT,
    tolerance: 2 * MICROSIEVERT,
    unit: "uSv/week",
    notes: "Published Hcg = 65.3 uSv/week",
  },
  {
    name: "Capture gamma IMRT",
    actual: mazeCaptureGammaImrt.weeklyCaptureGammaDoseEquivalent,
    expected: 170 * MICROSIEVERT,
    tolerance: 4 * MICROSIEVERT,
    unit: "uSv/week",
    notes: "Published Hcg = 170 uSv/week",
  },
  {
    name: "Maze door neutron conventional",
    actual: mazeNeutronKerseyConventional.weeklyNeutronDoseEquivalent,
    expected: 765 * MICROSIEVERT,
    tolerance: 0.5 * MICROSIEVERT,
    unit: "uSv/week",
    notes: "Published Kersey Hn = 765 uSv/week",
  },
  {
    name: "Maze door neutron IMRT",
    actual: mazeNeutronKerseyImrt.weeklyNeutronDoseEquivalent,
    expected: 1989 * MICROSIEVERT,
    tolerance: 0.5 * MICROSIEVERT,
    unit: "uSv/week",
    notes: "Published Kersey Hn = 1,989 uSv/week",
  },
  {
    name: "Maze door neutron Wu-McGinley",
    actual: mazeNeutronWu.weeklyNeutronDoseEquivalent,
    expected: 360 * MICROSIEVERT,
    tolerance: 30 * MICROSIEVERT,
    unit: "uSv/week",
    notes: "Published alternative estimate: about 0.8e-6 Sv/Gy",
  },
  {
    name: "Maze barrier photon slant",
    actual: mazeBarrier.photonRequiredSlantThickness,
    expected: 910,
    tolerance: 10,
    unit: "mm",
    notes: "Published leakage-photon slant thickness: 91 cm",
  },
  {
    name: "Maze barrier recommended thickness",
    actual: mazeBarrier.recommendedThickness,
    expected: 1250 * Math.cos((MAZE_BARRIER_ANGLE_DEGREES * Math.PI) / 180),
    tolerance: 12,
    unit: "mm",
    notes: "Published recommended concrete thickness: 96 cm",
  },
  {
    name: "Door lead conventional",
    actual: doorShieldingConventional.leadThicknessMm,
    expected: 7.3,
    tolerance: 0.6,
    unit: "mm",
    notes: "Published lead requirement: about 7 mm",
  },
  {
    name: "Door BPE conventional",
    actual: doorShieldingConventional.bpeThicknessMm,
    expected: 53.6,
    tolerance: 0.6,
    unit: "mm",
    notes: "Published BPE requirement: 53.6 mm",
  },
  {
    name: "Door lead IMRT",
    actual: doorShieldingImrt.leadThicknessMm,
    expected: 32,
    tolerance: 1,
    unit: "mm",
    notes: "Published lead requirement: 32 mm",
  },
  {
    name: "Door BPE IMRT",
    actual: doorShieldingImrt.bpeThicknessMm,
    expected: 72,
    tolerance: 1,
    unit: "mm",
    notes: "Published BPE requirement: 72 mm",
  },
];

const scalarResults = scalarCases.map((testCase) => {
  const evaluation = evaluateScalarCase(testCase);

  return {
    Case: testCase.name,
    Expected: formatScalar(testCase.expected, testCase.unit),
    Actual: formatScalar(testCase.actual, testCase.unit),
    Delta: formatScalar(evaluation.difference, testCase.unit),
    Status: evaluation.passed ? "PASS" : "FAIL",
    Notes: testCase.notes,
  };
});

console.log("Secondary barriers");
console.table(secondaryBarrierResults);

console.log("Maze and door components");
console.table(scalarResults);

if (
  secondaryBarrierResults.some((result) => result.Status === "FAIL") ||
  scalarResults.some((result) => result.Status === "FAIL")
) {
  process.exitCode = 1;
}
