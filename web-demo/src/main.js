import * as THREE from 'three';

// === CONFIG ===
const CONFIG = {
  oceanSize: 800,
  oceanSegments: 128,
  waveScale: 1.0,
  boatMass: 1200,
};

const WAVES = [
  { dir: new THREE.Vector2(1, 0.3).normalize(), steep: 0.12, len: 80, speed: 1.2, amp: 1.8 },
  { dir: new THREE.Vector2(0.7, 0.7).normalize(), steep: 0.08, len: 45, speed: 1.0, amp: 1.0 },
  { dir: new THREE.Vector2(-0.3, 1).normalize(), steep: 0.06, len: 25, speed: 0.8, amp: 0.5 },
  { dir: new THREE.Vector2(0.2, -1).normalize(), steep: 0.04, len: 15, speed: 0.7, amp: 0.3 },
  { dir: new THREE.Vector2(-0.8, -0.4).normalize(), steep: 0.05, len: 55, speed: 1.1, amp: 0.9 },
  { dir: new THREE.Vector2(0.5, -0.8).normalize(), steep: 0.03, len: 10, speed: 0.6, amp: 0.18 },
];

let time = 0;
let stormFactor = 0; // 0-1
let targetStorm = 0;

// === THREE SETUP ===
const canvas = document.getElementById('canvas');
const scene = new THREE.Scene();
scene.fog = new THREE.FogExp2(0x0a2a4a, 0.0012);
scene.background = new THREE.Color(0x0a2a4a);

const renderer = new THREE.WebGLRenderer({ canvas, antialias: true, powerPreference: "high-performance" });
renderer.setSize(window.innerWidth, window.innerHeight);
renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
renderer.shadowMap.enabled = true;
renderer.shadowMap.type = THREE.PCFSoftShadowMap;
renderer.toneMapping = THREE.ACESFilmicToneMapping;
renderer.toneMappingExposure = 1.1;

const camera = new THREE.PerspectiveCamera(65, window.innerWidth/window.innerHeight, 0.1, 3000);
let camYaw = -0.4, camPitch = 0.35, camDist = 22;
let camTargetYaw = camYaw, camTargetPitch = camPitch, camTargetDist = camDist;
let isDragging = false;
let lastMouse = {x:0,y:0};

canvas.addEventListener('mousedown', e=>{
  if(e.button===2) { isDragging=true; lastMouse={x:e.clientX,y:e.clientY}; }
});
window.addEventListener('mouseup', ()=> isDragging=false);
window.addEventListener('mousemove', e=>{
  if(!isDragging) return;
  camTargetYaw -= (e.clientX-lastMouse.x)*0.005;
  camTargetPitch += (e.clientY-lastMouse.y)*0.005;
  camTargetPitch = Math.max(-0.2, Math.min(1.2, camTargetPitch));
  lastMouse={x:e.clientX,y:e.clientY};
});
canvas.addEventListener('wheel', e=>{
  camTargetDist += e.deltaY*0.02;
  camTargetDist = Math.max(6, Math.min(60, camTargetDist));
  e.preventDefault();
},{passive:false});
canvas.addEventListener('contextmenu', e=>e.preventDefault());

window.addEventListener('resize', ()=>{
  camera.aspect = window.innerWidth/window.innerHeight;
  camera.updateProjectionMatrix();
  renderer.setSize(window.innerWidth, window.innerHeight);
});

// Lights
const sun = new THREE.DirectionalLight(0xffffff, 2.5);
sun.position.set(100, 80, 40);
sun.castShadow = true;
sun.shadow.mapSize.set(2048,2048);
sun.shadow.camera.near = 1; sun.shadow.camera.far = 400;
sun.shadow.camera.left = -150; sun.shadow.camera.right = 150;
sun.shadow.camera.top = 150; sun.shadow.camera.bottom = -150;
sun.shadow.bias = -0.0005;
scene.add(sun);
scene.add(new THREE.HemisphereLight(0x8ecfff, 0x0a2a4a, 0.8));
scene.add(new THREE.AmbientLight(0x4a7aaa, 0.4));

// === OCEAN GERSTNER MATH (same as Unity) ===
function getWave(x, z, t) {
  let dispX=0, dispY=0, dispZ=0;
  let velX=0, velY=0, velZ=0;
  let normX=0, normY=1, normZ=0;
  let tanX=1, tanY=0, tanZ=0;
  let binX=0, binY=0, binZ=1;

  for(let w of WAVES){
    const k = 2*Math.PI / w.len;
    const c = Math.sqrt(9.8 / k);
    const amp = w.amp * (1 + stormFactor*1.8) * CONFIG.waveScale;
    const speed = w.speed * (1 + stormFactor*0.6);
    const steep = w.steep;
    const dir = w.dir;

    const f = k * (dir.x * x + dir.y * z - c * t * speed);
    const sinF = Math.sin(f);
    const cosF = Math.cos(f);

    dispX += -dir.x * amp * sinF * steep;
    dispY += amp * cosF;
    dispZ += -dir.y * amp * sinF * steep;

    const wa = k * c * speed;
    velX += dir.x * amp * wa * cosF * steep;
    velY += -amp * wa * sinF;
    velZ += dir.y * amp * wa * cosF * steep;

    // derivatives for normal
    const ddx = -k * dir.x * dir.x * steep * amp * cosF;
    const ddy = -k * dir.x * amp * sinF;
    const ddz = -k * dir.x * dir.y * steep * amp * cosF;
    const ddx2 = -k * dir.y * dir.x * steep * amp * cosF;
    const ddy2 = -k * dir.y * amp * sinF;
    const ddz2 = -k * dir.y * dir.y * steep * amp * cosF;

    tanX += ddx; tanY += ddy; tanZ += ddz;
    binX += ddx2; binY += ddy2; binZ += ddz2;
  }

  // normal = cross(binormal, tangent)
  const nX = binY * tanZ - binZ * tanY;
  const nY = binZ * tanX - binX * tanZ;
  const nZ = binX * tanY - binY * tanX;
  const len = Math.sqrt(nX*nX + nY*nY + nZ*nZ) || 1;
  return {
    displacement: new THREE.Vector3(dispX, dispY, dispZ),
    normal: new THREE.Vector3(nX/len, nY/len, nZ/len),
    velocity: new THREE.Vector3(velX, velY, velZ),
    height: dispY
  };
}

// === OCEAN MESH ===
const oceanGeo = new THREE.PlaneGeometry(CONFIG.oceanSize, CONFIG.oceanSize, CONFIG.oceanSegments, CONFIG.oceanSegments);
oceanGeo.rotateX(-Math.PI/2);

const oceanMat = new THREE.ShaderMaterial({
  uniforms: {
    uTime: {value:0},
    uDeep: {value: new THREE.Color(0x021a33)},
    uShallow: {value: new THREE.Color(0x0a6a8a)},
    uStorm: {value:0},
  },
  vertexShader: `
    uniform float uTime;
    varying vec3 vWorldPos;
    varying vec3 vNormal;
    varying float vWaveHeight;
    varying float vFoam;

    // Gerstner inside shader for visual only (low freq for perf)
    vec3 gerstner(vec2 dir, float steep, float len, float amp, float speed, vec3 pos, float t, inout vec3 tangent, inout vec3 binormal){
      float k = 6.283185 / len;
      float c = sqrt(9.8 / k);
      float f = k * (dot(dir, pos.xz) - c * t * speed);
      float s = sin(f);
      float co = cos(f);
      vec3 disp;
      disp.x = -dir.x * amp * s * steep;
      disp.y = amp * co;
      disp.z = -dir.y * amp * s * steep;
      tangent.x += -k * dir.x * dir.x * steep * amp * co;
      tangent.y += -k * dir.x * amp * s;
      tangent.z += -k * dir.x * dir.y * steep * amp * co;
      binormal.x += -k * dir.y * dir.x * steep * amp * co;
      binormal.y += -k * dir.y * amp * s;
      binormal.z += -k * dir.y * dir.y * steep * amp * co;
      return disp;
    }

    void main(){
      vec3 pos = position;
      vec3 tangent = vec3(1.,0.,0.);
      vec3 binormal = vec3(0.,0.,1.);
      vec3 disp = vec3(0.);

      float t = uTime;
      disp += gerstner(normalize(vec2(1.,0.3)), 0.12, 80., 1.8, 1.2, pos, t, tangent, binormal);
      disp += gerstner(normalize(vec2(0.7,0.7)), 0.08, 45., 1.0, 1.0, pos, t, tangent, binormal);
      disp += gerstner(normalize(vec2(-0.3,1.)), 0.06, 25., 0.5, 0.8, pos, t, tangent, binormal);
      disp += gerstner(normalize(vec2(0.2,-1.)), 0.04, 15., 0.3, 0.7, pos, t, tangent, binormal);
      disp += gerstner(normalize(vec2(-0.8,-0.4)), 0.05, 55., 0.9, 1.1, pos, t, tangent, binormal);

      pos += disp;
      vec3 norm = normalize(cross(binormal, tangent));

      vWorldPos = (modelMatrix * vec4(pos,1.)).xyz;
      vNormal = norm;
      vWaveHeight = disp.y;
      vFoam = smoothstep(1.0, 2.5, disp.y);

      gl_Position = projectionMatrix * modelViewMatrix * vec4(pos,1.);
    }
  `,
  fragmentShader: `
    uniform vec3 uDeep;
    uniform vec3 uShallow;
    uniform float uStorm;
    varying vec3 vWorldPos;
    varying vec3 vNormal;
    varying float vWaveHeight;
    varying float vFoam;

    void main(){
      vec3 viewDir = normalize(cameraPosition - vWorldPos);
      float fresnel = pow(1.0 - max(0., dot(vNormal, viewDir)), 4.5);

      float heightFactor = clamp(vWaveHeight*0.25 + 0.5, 0., 1.);
      vec3 base = mix(uDeep, uShallow, heightFactor + fresnel*0.35);

      // sun specular
      vec3 lightDir = normalize(vec3(0.6,0.8,0.3));
      float spec = pow(max(0., dot(reflect(-lightDir, vNormal), viewDir)), 120.);
      base += spec * vec3(1.0,0.95,0.8) * 0.9;

      // foam
      base = mix(base, vec3(1.), vFoam*0.65);

      // storm darkening
      base = mix(base, base*0.6, uStorm*0.5);

      // depth fog fake
      float dist = length(vWorldPos.xz);
      float fog = 1.0 - exp(-dist*0.0008);
      base = mix(base, vec3(0.04,0.16,0.29), fog*0.4);

      gl_FragColor = vec4(base, 1.);
    }
  `,
});

const ocean = new THREE.Mesh(oceanGeo, oceanMat);
ocean.receiveShadow = true;
scene.add(ocean);

// infinite ocean illusion - move with boat
let oceanTiles = [];
function createOceanTiles(){
  // 3x3 tiles
  for(let x=-1;x<=1;x++) for(let z=-1;z<=1;z++){
    if(x===0 && z===0) continue;
    const m = ocean.clone();
    m.position.set(x*CONFIG.oceanSize*0.95, 0, z*CONFIG.oceanSize*0.95);
    scene.add(m);
    oceanTiles.push(m);
  }
}
createOceanTiles();

// === BOAT ===
function createBoat(){
  const group = new THREE.Group();

  // hull - custom shape
  const hullGeo = new THREE.BoxGeometry(3, 1.2, 9, 4,1,6);
  const pos = hullGeo.attributes.position;
  for(let i=0;i<pos.count;i++){
    const x = pos.getX(i), y = pos.getY(i), z = pos.getZ(i);
    // taper nose
    let taper = 1;
    if(z>2) taper = 1 - (z-2)/7 * 0.85;
    if(z<-2.5) taper = 0.8;
    pos.setX(i, x * Math.max(0.15, taper));
    // bottom curve
    if(y<0) pos.setX(i, pos.getX(i)*0.85);
    if(y<0 && Math.abs(x)<1) pos.setY(i, y - Math.abs(x)*0.15);
  }
  pos.needsUpdate=true;
  hullGeo.computeVertexNormals();
  const hullMat = new THREE.MeshStandardMaterial({color:0x8b4513, roughness:0.7, metalness:0.1});
  const hull = new THREE.Mesh(hullGeo, hullMat);
  hull.castShadow=true; hull.receiveShadow=true;
  hull.position.y=0.2;
  group.add(hull);

  // deck
  const deck = new THREE.Mesh(new THREE.BoxGeometry(2.6,0.2,7), new THREE.MeshStandardMaterial({color:0xd2b48c}));
  deck.position.y=0.9; deck.castShadow=true;
  group.add(deck);

  // cabin
  const cabin = new THREE.Mesh(new THREE.BoxGeometry(2,1.2,2.5), new THREE.MeshStandardMaterial({color:0xf5f5dc}));
  cabin.position.set(0,1.6,-1.2); cabin.castShadow=true;
  group.add(cabin);

  // mast
  const mast = new THREE.Mesh(new THREE.CylinderGeometry(0.07,0.09,6,8), new THREE.MeshStandardMaterial({color:0x5a3a1a}));
  mast.position.set(0,3.5,1.2);
  group.add(mast);

  // sail
  const sailGeo = new THREE.PlaneGeometry(2.8,4);
  const sailMat = new THREE.MeshStandardMaterial({color:0xfffff0, side:THREE.DoubleSide, roughness:0.9});
  const sail = new THREE.Mesh(sailGeo, sailMat);
  sail.position.set(0,3.6,1.25);
  sail.rotation.y=Math.PI/2;
  sail.castShadow=true;
  group.add(sail);

  // rudder visual
  const rudder = new THREE.Mesh(new THREE.BoxGeometry(0.15,0.8,0.6), new THREE.MeshStandardMaterial({color:0x333333}));
  rudder.position.set(0,-0.2,-4.6);
  group.add(rudder);
  group.userData.rudder = rudder;

  // buoyancy points (relative)
  group.userData.buoyancyPoints = [
    new THREE.Vector3(0,-0.4,3.2),   // bow
    new THREE.Vector3(0,-0.4,-3.2),  // stern
    new THREE.Vector3(0,-0.7,0),     // center
    new THREE.Vector3(1.1,-0.4,0.8), // starboard
    new THREE.Vector3(-1.1,-0.4,0.8),// port
    new THREE.Vector3(0,-0.5,-1.5),
  ];

  return group;
}

const boat = createBoat();
scene.add(boat);

// Physics state
const physics = {
  pos: new THREE.Vector3(0,2,0),
  vel: new THREE.Vector3(0,0,0),
  rot: new THREE.Euler(0,0,0),
  angVel: new THREE.Vector3(0,0,0),
  mass: CONFIG.boatMass,
  throttle:0,
  targetThrottle:0,
  rudder:0,
  targetRudder:0,
  speed:0,
};

// Input
const keys = {};
window.addEventListener('keydown', e=>{
  keys[e.code]=true;
  if(e.code==='KeyR') resetGame();
});
window.addEventListener('keyup', e=> keys[e.code]=false);

// === BUOYS ===
let buoys = [];
let collectedCount = 0;
const totalBuoys = 8;

function spawnBuoys(){
  buoys.forEach(b=> scene.remove(b.mesh));
  buoys=[];
  collectedCount=0;
  updateBuoyUI();
  for(let i=0;i<totalBuoys;i++){
    const angle = (i/totalBuoys)*Math.PI*2 + Math.random()*0.5;
    const dist = 40 + Math.random()*120;
    const x = Math.cos(angle)*dist;
    const z = Math.sin(angle)*dist;

    const group = new THREE.Group();
    const floatPart = new THREE.Mesh(new THREE.CylinderGeometry(0.6,0.6,0.3,16), new THREE.MeshStandardMaterial({color:0xffcc33, emissive:0xffaa00, emissiveIntensity:0.2}));
    floatPart.castShadow=true;
    const top = new THREE.Mesh(new THREE.CylinderGeometry(0.2,0.2,1.2,12), new THREE.MeshStandardMaterial({color:0xff4444}));
    top.position.y=0.7;
    group.add(floatPart, top);
    group.position.set(x,0,z);
    scene.add(group);

    buoys.push({mesh:group, basePos:new THREE.Vector3(x,0,z), collected:false, bob: Math.random()*Math.PI*2});
  }
}

function updateBuoyUI(){
  const cont = document.getElementById('buoy-counter');
  cont.innerHTML='';
  for(let i=0;i<totalBuoys;i++){
    const d=document.createElement('div');
    d.className='buoy-dot'+(i<collectedCount?' collected':'');
    cont.appendChild(d);
  }
}

// === WAKE PARTICLES ===
const wakeGeo = new THREE.BufferGeometry();
const wakeCount=300;
const wakePos = new Float32Array(wakeCount*3);
const wakeVel = new Float32Array(wakeCount*3);
const wakeLife = new Float32Array(wakeCount);
for(let i=0;i<wakeCount;i++){ wakeLife[i]=0; }
wakeGeo.setAttribute('position', new THREE.BufferAttribute(wakePos,3));
const wakeMat = new THREE.PointsMaterial({size:0.6, color:0xffffff, transparent:true, opacity:0.6, sizeAttenuation:true});
const wakePoints = new THREE.Points(wakeGeo, wakeMat);
scene.add(wakePoints);

function emitWake(pos, intensity=1){
  for(let i=0;i<wakeCount;i++) if(wakeLife[i]<=0){
    wakePos[i*3]=pos.x + (Math.random()-0.5)*1.5;
    wakePos[i*3+1]=0.1;
    wakePos[i*3+2]=pos.z + (Math.random()-0.5)*1.5;
    wakeVel[i*3]= (Math.random()-0.5)*0.5;
    wakeVel[i*3+1]=0;
    wakeVel[i*3+2]= (Math.random()-0.5)*0.5;
    wakeLife[i]= 2.5 * intensity;
    break;
  }
}

// === GAME LOOP ===
let lastTime=0;
let fps=60, fpsAcc=0, fpsCount=0;
const fixedStep=1/60;
let accumulator=0;

function resetGame(){
  physics.pos.set(0,2,0);
  physics.vel.set(0,0,0);
  physics.angVel.set(0,0,0);
  physics.rot.set(0,0,0);
  physics.throttle=0; physics.targetThrottle=0;
  spawnBuoys();
  stormFactor=0; targetStorm=0;
}

function updatePhysics(dt){
  time += dt * (1+stormFactor*0.8);
  oceanMat.uniforms.uTime.value = time;
  oceanMat.uniforms.uStorm.value = stormFactor;

  // Input -> target
  let vert=0, horiz=0;
  if(keys['KeyW']||keys['ArrowUp']) vert+=1;
  if(keys['KeyS']||keys['ArrowDown']) vert-=1;
  if(keys['KeyA']||keys['ArrowLeft']) horiz-=1;
  if(keys['KeyD']||keys['ArrowRight']) horiz+=1;
  if(keys['Space']) { physics.vel.multiplyScalar(0.92); physics.angVel.multiplyScalar(0.85); }

  physics.targetThrottle = vert;
  physics.targetRudder = horiz;

  // smooth
  physics.throttle += (physics.targetThrottle - physics.throttle) * Math.min(1, dt*2.5);
  physics.rudder += (physics.targetRudder - physics.rudder) * Math.min(1, dt*4);

  // rudder visual
  if(boat.userData.rudder){
    boat.userData.rudder.rotation.y = physics.rudder * 0.9;
  }

  // === BUOYANCY ===
  let totalForce = new THREE.Vector3(0, -9.81*physics.mass*0.6, 0); // gravity reduced for float
  let totalTorque = new THREE.Vector3(0,0,0);
  let submerged=0;
  let avgWaterHeight=0;

  const worldPoints = physics.boatWorldPoints || [];
  worldPoints.length=0;

  for(let localPoint of boat.userData.buoyancyPoints){
    // world point = boat pos + rotated local
    const wp = localPoint.clone().applyEuler(physics.rot).add(physics.pos);
    worldPoints.push(wp);

    const wave = getWave(wp.x, wp.z, time);
    avgWaterHeight += wave.height;
    const depth = wave.height - wp.y;
    if(depth>0){
      submerged++;
      const buoyancy = depth * 18 * physics.mass / boat.userData.buoyancyPoints.length;
      const buoyForce = wave.normal.clone().multiplyScalar(buoyancy * (0.6 + 0.4*1)); // lerp up/normal
      buoyForce.y = Math.max(buoyForce.y, buoyancy*0.7);
      totalForce.add(buoyForce);

      // drag
      const pointVel = new THREE.Vector3().copy(physics.vel).add(
        new THREE.Vector3().crossVectors(physics.angVel, localPoint.clone().applyEuler(physics.rot))
      );
      const relVel = pointVel.clone().sub(wave.velocity);
      const drag = relVel.clone().multiplyScalar(-3.5 * Math.min(1, depth));
      totalForce.add(drag);
      totalTorque.add(new THREE.Vector3().crossVectors(localPoint.clone().applyEuler(physics.rot), drag.clone().multiplyScalar(0.1)));

      // torque from buoyancy offset
      const offset = localPoint.clone().applyEuler(physics.rot);
      totalTorque.add(new THREE.Vector3().crossVectors(offset, buoyForce.clone().multiplyScalar(0.08)));
    }
  }
  avgWaterHeight/=boat.userData.buoyancyPoints.length;
  physics.avgWaveHeight = avgWaterHeight;
  physics.boatWorldPoints = worldPoints;

  const submergedRatio = submerged / boat.userData.buoyancyPoints.length;

  // Engine force
  const forward = new THREE.Vector3(0,0,1).applyEuler(physics.rot);
  const forwardSpeed = physics.vel.dot(forward);
  const speedFactor = 1 - Math.min(1, Math.abs(forwardSpeed)/15);
  const engineForce = forward.clone().multiplyScalar(physics.throttle * 9000 * speedFactor);
  totalForce.add(engineForce);

  // Rudder torque (only when moving)
  const rudderEff = Math.min(1, Math.abs(forwardSpeed)/2.5) * (1+submergedRatio);
  const dirSign = Math.abs(physics.throttle)>0.1 ? Math.sign(physics.throttle) : Math.sign(forwardSpeed) || 1;
  totalTorque.y += physics.rudder * 3200 * rudderEff * dirSign;

  // lateral drag (anti slide)
  const right = new THREE.Vector3(1,0,0).applyEuler(physics.rot);
  const lateral = right.clone().multiplyScalar(physics.vel.dot(right));
  totalForce.add(lateral.multiplyScalar(-3.5));

  // water angular drag
  if(submergedRatio>0.1){
    totalTorque.add(physics.angVel.clone().multiplyScalar(-1200*submergedRatio));
    // auto stabilization
    totalTorque.x += -physics.rot.x * 800 * submergedRatio;
    totalTorque.z += -physics.rot.z * 800 * submergedRatio;
  }

  // integrate
  const accel = totalForce.clone().divideScalar(physics.mass);
  physics.vel.add(accel.multiplyScalar(dt));

  // soft max speed
  if(physics.vel.length()>18){
    physics.vel.multiplyScalar(0.995);
  }

  physics.pos.add(physics.vel.clone().multiplyScalar(dt));

  // angular
  const angAccel = totalTorque.clone().divideScalar(physics.mass*0.6);
  physics.angVel.add(angAccel.multiplyScalar(dt));
  physics.rot.x += physics.angVel.x * dt;
  physics.rot.y += physics.angVel.y * dt;
  physics.rot.z += physics.angVel.z * dt;

  // apply to mesh
  boat.position.copy(physics.pos);
  boat.rotation.copy(physics.rot);

  // infinite ocean follow
  ocean.position.x = physics.pos.x;
  ocean.position.z = physics.pos.z;
  oceanTiles.forEach((t,i)=>{
    const ox = Math.round(physics.pos.x / CONFIG.oceanSize) * CONFIG.oceanSize;
    const oz = Math.round(physics.pos.z / CONFIG.oceanSize) * CONFIG.oceanSize;
    // simple reposition based on grid
    const gx = (i%3)-1, gz = Math.floor(i/3)-1;
    t.position.set(ox + gx*CONFIG.oceanSize*0.95, 0, oz + gz*CONFIG.oceanSize*0.95);
  });

  // wake
  if(physics.vel.length()>1.5 && submergedRatio>0.3){
    const stern = new THREE.Vector3(0,0,-4).applyEuler(physics.rot).add(physics.pos);
    emitWake(stern, physics.vel.length()*0.1);
  }

  // buoys update
  buoys.forEach(b=>{
    if(b.collected) return;
    const wave = getWave(b.basePos.x, b.basePos.z, time);
    b.mesh.position.y = wave.height + Math.sin(time*1.3 + b.bob)*0.25;
    b.mesh.position.x = b.basePos.x + wave.displacement.x;
    b.mesh.position.z = b.basePos.z + wave.displacement.z;
    b.mesh.rotation.y += dt*0.5;
    // check collect
    if(physics.pos.distanceTo(b.mesh.position)<5){
      b.collected=true;
      scene.remove(b.mesh);
      collectedCount++;
      updateBuoyUI();
      if(collectedCount>=totalBuoys){
        document.getElementById('center-msg').innerHTML = "🏆 ПОБЕДА! Все буи собраны! Нажми R для рестарта";
        setTimeout(()=> spawnBuoys(), 3000);
      }
    }
  });

  // storm logic
  if(Math.random()<0.001 && targetStorm<0.1){
    targetStorm=1;
    document.getElementById('storm').classList.add('active');
    document.getElementById('center-msg').innerHTML="⛈️ ШТОРМ НАДВИГАЕТСЯ! Держись!";
  }
  if(targetStorm>0.5){
    // auto end after 20 sec
    if(Math.random()<0.0015) targetStorm=0;
  }
  stormFactor += (targetStorm - stormFactor) * dt * 0.3;
  if(stormFactor<0.05 && targetStorm===0){
    document.getElementById('storm').classList.remove('active');
  }

  // wake particles update
  const pPos = wakeGeo.attributes.position.array;
  for(let i=0;i<wakeCount;i++){
    if(wakeLife[i]>0){
      pPos[i*3]+= wakeVel[i*3]*dt;
      pPos[i*3+2]+= wakeVel[i*3+2]*dt;
      pPos[i*3+1]+= Math.sin(time*2 + i)*0.01;
      wakeLife[i]-=dt;
      if(wakeLife[i]<=0){
        pPos[i*3+1]=-100;
      }
    }
  }
  wakeGeo.attributes.position.needsUpdate=true;
  wakeMat.opacity = 0.6;

  physics.speed = physics.vel.length()*1.94384; // m/s to knots
}

function animate(t){
  requestAnimationFrame(animate);
  const dt = Math.min(0.05, (t - lastTime)/1000);
  lastTime=t;
  accumulator+=dt;

  // FPS
  fpsAcc+=dt; fpsCount++;
  if(fpsAcc>0.5){ fps=Math.round(fpsCount/fpsAcc); fpsAcc=0; fpsCount=0; }

  while(accumulator>=fixedStep){
    updatePhysics(fixedStep);
    accumulator-=fixedStep;
  }

  // camera smoothing
  camYaw += (camTargetYaw - camYaw)*0.08;
  camPitch += (camTargetPitch - camPitch)*0.08;
  camDist += (camTargetDist - camDist)*0.08;

  const targetPos = physics.pos.clone().add(new THREE.Vector3(0,2,0));
  const rot = new THREE.Euler(camPitch, camYaw + physics.rot.y, 0, 'YXZ');
  const offset = new THREE.Vector3(0,0,-camDist).applyEuler(rot).add(new THREE.Vector3(0, camPitch*4,0));
  const desiredCamPos = targetPos.clone().add(offset);
  camera.position.lerp(desiredCamPos, 0.08);
  camera.lookAt(targetPos);

  // UI
  document.getElementById('speed').textContent = `Скорость: ${physics.speed.toFixed(1)} узл (${(physics.vel.length()*3.6).toFixed(1)} км/ч)`;
  document.getElementById('wave').textContent = `Высота волны: ${(physics.avgWaveHeight||0).toFixed(2)} м ${stormFactor>0.5?'⛈️ ШТОРМ':''}`;
  document.getElementById('pos').textContent = `Коорд: ${physics.pos.x.toFixed(0)}, ${physics.pos.z.toFixed(0)}`;
  document.getElementById('fps').textContent = `FPS: ${fps} | Буи: ${collectedCount}/${totalBuoys}`;

  renderer.render(scene, camera);
}

// init
document.getElementById('progress').style.width='30%';
setTimeout(()=>{
  document.getElementById('progress').style.width='80%';
  spawnBuoys();
  resetGame();
  document.getElementById('progress').style.width='100%';
  setTimeout(()=>{
    document.getElementById('loading').style.opacity='0';
    setTimeout(()=> document.getElementById('loading').remove(), 800);
  },400);
  lastTime=performance.now();
  animate(lastTime);
}, 600);

// prevent scroll
document.body.addEventListener('touchmove', e=> e.preventDefault(), {passive:false});
