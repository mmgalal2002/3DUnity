// Convert a static, self-contained glTF binary scene to Room Studio's editable
// ModelData format. The original GLB remains in Assets/SourceModels.
// Usage: node Tools/convert-glb.mjs input.glb Assets/ModelData/Name.json
import { readFileSync, writeFileSync } from 'node:fs';
import { createHash } from 'node:crypto';

const [source, destination, option] = process.argv.slice(2);
const flatten = option === '--flatten';
if (option && !flatten) throw new Error('Unknown conversion option.');
if (!source || !destination) throw new Error('Expected source GLB and destination ModelData JSON.');
const bytes = readFileSync(source);
if (bytes.toString('ascii', 0, 4) !== 'glTF' || bytes.readUInt32LE(4) !== 2 || bytes.readUInt32LE(8) !== bytes.length)
  throw new Error('Expected a complete glTF 2.0 binary file.');
let cursor = 12;
const chunks = [];
while (cursor < bytes.length) {
  const size = bytes.readUInt32LE(cursor), type = bytes.readUInt32LE(cursor + 4);
  const start = cursor + 8;
  if (start + size > bytes.length) throw new Error('Truncated GLB chunk.');
  chunks.push({ type, data: bytes.subarray(start, start + size) });
  cursor = start + size;
}
if (chunks[0]?.type !== 0x4e4f534a || chunks[1]?.type !== 0x004e4942)
  throw new Error('Expected JSON and embedded binary chunks.');
const gltf = JSON.parse(chunks[0].data.toString('utf8').trimEnd());
const binary = chunks[1].data;
if (gltf.buffers?.length !== 1 || gltf.buffers[0].uri || gltf.buffers[0].byteLength > binary.length ||
    gltf.extensionsRequired?.length || gltf.skins?.length)
  throw new Error('The source uses an unsupported buffer, extension, or skin.');

const dimensions = { SCALAR: 1, VEC2: 2, VEC3: 3, VEC4: 4 };
const componentBytes = { 5123: 2, 5125: 4, 5126: 4 };
function readAccessor(id) {
  const accessor = gltf.accessors[id];
  if (!accessor || accessor.sparse || accessor.normalized || !dimensions[accessor.type] || !componentBytes[accessor.componentType])
    throw new Error(`Unsupported accessor ${id}.`);
  const view = gltf.bufferViews[accessor.bufferView];
  if (!view || view.buffer !== 0) throw new Error(`Missing embedded view for accessor ${id}.`);
  const width = dimensions[accessor.type], size = componentBytes[accessor.componentType];
  const stride = view.byteStride ?? width * size;
  if (stride < width * size) throw new Error(`Invalid stride in accessor ${id}.`);
  const offset = (view.byteOffset ?? 0) + (accessor.byteOffset ?? 0);
  if (offset + (accessor.count - 1) * stride + width * size > (view.byteOffset ?? 0) + view.byteLength)
    throw new Error(`Accessor ${id} exceeds its buffer view.`);
  const result = new Array(accessor.count * width);
  for (let row = 0; row < accessor.count; row++) for (let column = 0; column < width; column++) {
    const at = offset + row * stride + column * size;
    result[row * width + column] = accessor.componentType === 5126 ? binary.readFloatLE(at)
      : accessor.componentType === 5123 ? binary.readUInt16LE(at) : binary.readUInt32LE(at);
  }
  return result;
}
function handedness(vector) { return [vector[0], vector[1], -vector[2]]; }
const materials = (gltf.materials ?? []).map((m, index) => {
  const pbr = m.pbrMetallicRoughness ?? {};
  if (pbr.baseColorTexture || m.normalTexture || m.emissiveTexture)
    throw new Error(`Textured material ${index} is unsupported by ModelData.`);
  return { name: m.name ?? `Material ${index}`, color: pbr.baseColorFactor ?? [1, 1, 1, 1],
    metallic: pbr.metallicFactor ?? 1, roughness: pbr.roughnessFactor ?? 1,
    emission: m.emissiveFactor ?? [0, 0, 0] };
});
if (!materials.length) materials.push({ name: 'Default', color: [1, 1, 1, 1], metallic: 0, roughness: 1, emission: [0, 0, 0] });

const parts = [];
const seen = new Set();
const identity = [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1];
function nodeMatrix(node) {
  if (node.matrix) {
    if (node.matrix.length !== 16 || node.matrix.some(value => !Number.isFinite(value)) ||
        node.matrix[3] !== 0 || node.matrix[7] !== 0 || node.matrix[11] !== 0 || node.matrix[15] !== 1)
      throw new Error('Expected a finite affine node matrix.');
    return node.matrix;
  }
  const [axisX, axisY, axisZ, scalar] = node.rotation ?? [0, 0, 0, 1];
  const [scaleX, scaleY, scaleZ] = node.scale ?? [1, 1, 1];
  const [translateX, translateY, translateZ] = node.translation ?? [0, 0, 0];
  return [
    (1 - 2 * (axisY * axisY + axisZ * axisZ)) * scaleX, 2 * (axisX * axisY + axisZ * scalar) * scaleX, 2 * (axisX * axisZ - axisY * scalar) * scaleX, 0,
    2 * (axisX * axisY - axisZ * scalar) * scaleY, (1 - 2 * (axisX * axisX + axisZ * axisZ)) * scaleY, 2 * (axisY * axisZ + axisX * scalar) * scaleY, 0,
    2 * (axisX * axisZ + axisY * scalar) * scaleZ, 2 * (axisY * axisZ - axisX * scalar) * scaleZ, (1 - 2 * (axisX * axisX + axisY * axisY)) * scaleZ, 0,
    translateX, translateY, translateZ, 1
  ];
}
function multiply(first, second) {
  const result = new Array(16).fill(0);
  for (let column = 0; column < 4; column++) for (let row = 0; row < 4; row++)
    for (let inner = 0; inner < 4; inner++) result[column * 4 + row] += first[inner * 4 + row] * second[column * 4 + inner];
  return result;
}
function bake(matrix, vertices, normals) {
  const [firstX, firstY, firstZ, , secondX, secondY, secondZ, , thirdX, thirdY, thirdZ] = matrix;
  const determinant = firstX * (secondY * thirdZ - thirdY * secondZ) - secondX * (firstY * thirdZ - thirdY * firstZ) + thirdX * (firstY * secondZ - secondY * firstZ);
  if (!Number.isFinite(determinant) || determinant === 0) throw new Error('Cannot flatten a singular node transform.');
  for (let offset = 0; offset < vertices.length; offset += 3) {
    const [pointX, pointY, pointZ] = vertices.slice(offset, offset + 3);
    vertices[offset] = firstX * pointX + secondX * pointY + thirdX * pointZ + matrix[12];
    vertices[offset + 1] = firstY * pointX + secondY * pointY + thirdY * pointZ + matrix[13];
    vertices[offset + 2] = firstZ * pointX + secondZ * pointY + thirdZ * pointZ + matrix[14];
  }
  for (let offset = 0; offset < normals.length; offset += 3) {
    const [normalX, normalY, normalZ] = normals.slice(offset, offset + 3);
    const transformedX = ((secondY * thirdZ - thirdY * secondZ) * normalX + (thirdY * firstZ - firstY * thirdZ) * normalY + (firstY * secondZ - secondY * firstZ) * normalZ) / determinant;
    const transformedY = ((thirdX * secondZ - secondX * thirdZ) * normalX + (firstX * thirdZ - thirdX * firstZ) * normalY + (secondX * firstZ - firstX * secondZ) * normalZ) / determinant;
    const transformedZ = ((secondX * thirdY - thirdX * secondY) * normalX + (thirdX * firstY - firstX * thirdY) * normalY + (firstX * secondY - secondX * firstY) * normalZ) / determinant;
    const length = Math.hypot(transformedX, transformedY, transformedZ);
    if (!Number.isFinite(length) || length === 0) throw new Error('Invalid transformed normal.');
    normals[offset] = transformedX / length; normals[offset + 1] = transformedY / length; normals[offset + 2] = transformedZ / length;
  }
  if (vertices.some(value => !Number.isFinite(value))) throw new Error('Nonfinite flattened vertex.');
  return determinant;
}
function visit(nodeId, parentMatrix = identity) {
  if (seen.has(nodeId)) throw new Error('The scene contains a shared or cyclic node.');
  seen.add(nodeId);
  const node = gltf.nodes[nodeId];
  if (!node || !flatten && (node.matrix || node.children?.length)) throw new Error(`Node ${nodeId} needs unsupported hierarchy or matrix transforms.`);
  const worldMatrix = flatten ? multiply(parentMatrix, nodeMatrix(node)) : identity;
  for (const child of node.children ?? []) visit(child, worldMatrix);
  if (node.mesh === undefined) return;
  const mesh = gltf.meshes[node.mesh];
  const rotation = node.rotation ?? [0, 0, 0, 1];
  for (const [index, primitive] of mesh.primitives.entries()) {
    if ((primitive.mode ?? 4) !== 4 || primitive.targets?.length) throw new Error(`Unsupported primitive ${nodeId}/${index}.`);
    if (primitive.indices === undefined) throw new Error(`Primitive ${nodeId}/${index} needs indices.`);
    const vertices = readAccessor(primitive.attributes.POSITION);
    const normals = primitive.attributes.NORMAL === undefined ? [] : readAccessor(primitive.attributes.NORMAL);
    const indices = readAccessor(primitive.indices);
    if (vertices.length % 3 || normals.length && normals.length !== vertices.length || indices.length % 3)
      throw new Error(`Invalid triangle data at node ${nodeId}.`);
    const determinant = flatten ? bake(worldMatrix, vertices, normals) : 1;
    for (let n = 2; n < vertices.length; n += 3) vertices[n] = -vertices[n];
    for (let n = 2; n < normals.length; n += 3) normals[n] = -normals[n];
    if (determinant > 0) for (let n = 0; n < indices.length; n += 3) [indices[n + 1], indices[n + 2]] = [indices[n + 2], indices[n + 1]];
    if (indices.some(value => value >= vertices.length / 3)) throw new Error(`Out-of-range index at node ${nodeId}.`);
    parts.push({ name: `${node.name ?? mesh.name ?? `Mesh ${nodeId}`}${mesh.primitives.length > 1 ? ` ${index}` : ''}`,
      vertices, normals, indices, material: primitive.material ?? 0,
      position: flatten ? [0, 0, 0] : handedness(node.translation ?? [0, 0, 0]),
      rotation: flatten ? [0, 0, 0, 1] : [-rotation[0], -rotation[1], rotation[2], rotation[3]], scale: flatten ? [1, 1, 1] : node.scale ?? [1, 1, 1] });
  }
}
for (const nodeId of gltf.scenes[gltf.scene ?? 0].nodes) visit(nodeId);
if (!parts.length) throw new Error('The scene has no triangles.');
if (parts.some(p => p.material < 0 || p.material >= materials.length)) throw new Error('A primitive references an unknown material.');
writeFileSync(destination, JSON.stringify({ materials, parts }) + '\n');
const triangles = parts.reduce((sum, p) => sum + p.indices.length / 3, 0);
console.log(JSON.stringify({ source, destination, sourceSha256: createHash('sha256').update(bytes).digest('hex'),
  materials: materials.length, parts: parts.length, triangles }));
