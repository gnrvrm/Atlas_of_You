import * as THREE from "../vendor/three.module.js";

const states = new WeakMap();

export function renderBodyAtlas3d(canvas, payload) {
  if (!canvas) {
    return;
  }

  const state = states.get(canvas) ?? createState(canvas);
  states.set(canvas, state);
  updateScene(state, payload);
}

export function disposeBodyAtlas3d(canvas) {
  const state = states.get(canvas);
  if (!state) {
    return;
  }

  state.resizeObserver.disconnect();
  state.canvas.removeEventListener("pointerdown", state.onPointerDown);
  state.canvas.removeEventListener("pointermove", state.onPointerMove);
  state.canvas.removeEventListener("pointerup", state.onPointerUp);
  state.canvas.removeEventListener("pointerleave", state.onPointerUp);
  cancelAnimationFrame(state.animationFrame);
  disposeObject(state.root);
  state.renderer.dispose();
  states.delete(canvas);
}

function createState(canvas) {
  const renderer = new THREE.WebGLRenderer({
    canvas,
    alpha: true,
    antialias: true,
    preserveDrawingBuffer: true,
  });

  renderer.setClearColor(0x000000, 0);
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  renderer.outputColorSpace = THREE.SRGBColorSpace;

  const scene = new THREE.Scene();
  const camera = new THREE.PerspectiveCamera(34, 1, 0.1, 100);
  camera.position.set(0, 1.25, 6.1);
  camera.lookAt(0, 1.1, 0);

  const root = new THREE.Group();
  root.rotation.y = -0.18;
  scene.add(root);

  const ambient = new THREE.HemisphereLight(0xf8fbff, 0x31564f, 1.7);
  scene.add(ambient);

  const keyLight = new THREE.DirectionalLight(0xffffff, 2.35);
  keyLight.position.set(2.5, 4.5, 4);
  scene.add(keyLight);

  const rimLight = new THREE.DirectionalLight(0x8ddbd0, 1.15);
  rimLight.position.set(-3, 2.8, -3);
  scene.add(rimLight);

  const state = {
    animationFrame: 0,
    camera,
    canvas,
    dragStartX: 0,
    frameCount: 0,
    isDragging: false,
    renderer,
    root,
    rotationTarget: -0.18,
    scene,
  };

  state.onPointerDown = (event) => {
    state.isDragging = true;
    state.dragStartX = event.clientX;
    state.canvas.setPointerCapture?.(event.pointerId);
    state.canvas.dataset.dragging = "true";
  };

  state.onPointerMove = (event) => {
    if (!state.isDragging) {
      return;
    }

    const delta = event.clientX - state.dragStartX;
    state.dragStartX = event.clientX;
    state.rotationTarget += delta * 0.008;
    state.canvas.dataset.userRotated = "true";
  };

  state.onPointerUp = (event) => {
    state.isDragging = false;
    state.canvas.releasePointerCapture?.(event.pointerId);
    state.canvas.dataset.dragging = "false";
  };

  canvas.addEventListener("pointerdown", state.onPointerDown);
  canvas.addEventListener("pointermove", state.onPointerMove);
  canvas.addEventListener("pointerup", state.onPointerUp);
  canvas.addEventListener("pointerleave", state.onPointerUp);

  state.resizeObserver = new ResizeObserver(() => resize(state));
  state.resizeObserver.observe(canvas);
  resize(state);
  animate(state);

  return state;
}

function updateScene(state, payload) {
  disposeObject(state.root);
  state.root.clear();

  const userHeightCm = toNumber(payload.userHeightCm, 175);
  const averageHeightCm = toNumber(payload.averageHeightCm, 170);
  const userWeightKg = toNumber(payload.userWeightKg, 75);
  const referenceWeightKg = Math.max(toNumber(payload.referenceWeightKg, userWeightKg), 1);
  const visualMin = Math.max(120, Math.min(userHeightCm, averageHeightCm) - 10);
  const visualMax = Math.min(230, Math.max(userHeightCm, averageHeightCm) + 10);
  const rangeMin = visualMax - visualMin < 20 ? visualMin - 8 : visualMin;
  const rangeMax = visualMax - visualMin < 20 ? visualMax + 8 : visualMax;
  const userVisualHeight = mapHeight(userHeightCm, rangeMin, rangeMax);
  const averageVisualHeight = mapHeight(averageHeightCm, rangeMin, rangeMax);
  const massRatio = Math.sqrt(THREE.MathUtils.clamp(userWeightKg / referenceWeightKg, 0.55, 1.85));

  const guideGroup = createGuideLayer({
    averageHeightCm,
    averageVisualHeight,
    userHeightCm,
    userVisualHeight,
  });
  state.root.add(guideGroup);

  const averageFigure = createHumanFigure({
    bodyColor: "#b9c7c2",
    eyeColor: "#596662",
    hairColor: "#89938f",
    handPreference: "",
    label: "Ort.",
    massScale: 1,
    opacity: 0.42,
    selectedSex: payload.selectedSex,
    visualHeight: averageVisualHeight,
  });
  averageFigure.position.x = -0.78;
  state.root.add(averageFigure);

  const userFigure = createHumanFigure({
    bloodColor: payload.bloodColor,
    bloodLabel: payload.bloodLabel,
    bodyColor: "#0f766e",
    eyeColor: payload.eyeColor,
    hairColor: payload.hairColor,
    handPreference: payload.handPreference,
    label: "Sen",
    massScale: THREE.MathUtils.clamp(massRatio, 0.78, 1.34),
    opacity: 0.96,
    selectedSex: payload.selectedSex,
    visualHeight: userVisualHeight,
  });
  userFigure.position.x = 0.78;
  state.root.add(userFigure);

  state.canvas.dataset.sceneReady = "true";
  state.canvas.dataset.figureCount = "2";
  state.canvas.dataset.userHeightCm = userHeightCm.toFixed(1);
  state.canvas.dataset.averageHeightCm = averageHeightCm.toFixed(1);
  state.canvas.dataset.massRatio = massRatio.toFixed(3);
}

function createHumanFigure(options) {
  const group = new THREE.Group();
  const height = options.visualHeight;
  const massScale = options.massScale;
  const isMale = options.selectedSex === "male";

  const skin = material(options.bodyColor, options.opacity, 0.62);
  const accent = material(options.bodyColor, Math.min(options.opacity + 0.04, 1), 0.52);
  const hair = material(options.hairColor || "#181512", options.opacity, 0.74);
  const eye = material(options.eyeColor || "#5a3825", 1, 0.5);
  const marker = material("#f9d160", 1, 0.38, 0.08);
  const blood = material(options.bloodColor || "#a43d55", 1, 0.44);

  const headRadius = height * 0.078;
  const hipY = height * 0.42;
  const shoulderY = height * 0.71;
  const chestY = height * 0.56;
  const headY = height - headRadius;
  const shoulderWidth = height * (isMale ? 0.34 : 0.3) * (0.92 + massScale * 0.08);
  const hipWidth = height * (isMale ? 0.22 : 0.3) * (0.9 + massScale * 0.1);
  const torsoRadius = height * 0.088 * massScale;
  const limbRadius = height * 0.032 * (0.95 + massScale * 0.05);

  const torso = cylinderBetween(
    new THREE.Vector3(0, hipY, 0),
    new THREE.Vector3(0, shoulderY, 0),
    torsoRadius,
    skin,
    32,
  );
  torso.scale.x = isMale ? 1.26 : 1.04;
  torso.scale.z = 0.72;
  group.add(torso);

  const shoulderBar = cylinderBetween(
    new THREE.Vector3(-shoulderWidth / 2, shoulderY, 0),
    new THREE.Vector3(shoulderWidth / 2, shoulderY, 0),
    limbRadius * 1.14,
    accent,
    24,
  );
  group.add(shoulderBar);

  const pelvis = new THREE.Mesh(new THREE.SphereGeometry(torsoRadius * 1.25, 28, 16), skin);
  pelvis.position.set(0, hipY, 0);
  pelvis.scale.set(isMale ? 1.15 : 1.42, 0.55, 0.7);
  group.add(pelvis);

  const neck = cylinderBetween(
    new THREE.Vector3(0, shoulderY + headRadius * 0.08, 0),
    new THREE.Vector3(0, headY - headRadius * 0.85, 0),
    limbRadius * 0.86,
    skin,
    18,
  );
  group.add(neck);

  const head = new THREE.Mesh(new THREE.SphereGeometry(headRadius, 32, 20), skin);
  head.position.set(0, headY, 0.02);
  head.scale.set(0.93, 1.08, 0.9);
  group.add(head);

  const hairCap = new THREE.Mesh(
    new THREE.SphereGeometry(headRadius * 1.03, 32, 12, 0, Math.PI * 2, 0, Math.PI * 0.55),
    hair,
  );
  hairCap.position.set(0, headY + headRadius * 0.06, 0.01);
  hairCap.scale.set(0.98, 1.04, 0.96);
  group.add(hairCap);

  if (options.opacity > 0.7) {
    for (const eyeX of [-headRadius * 0.34, headRadius * 0.34]) {
      const eyeDot = new THREE.Mesh(new THREE.SphereGeometry(headRadius * 0.095, 12, 8), eye);
      eyeDot.position.set(eyeX, headY + headRadius * 0.05, headRadius * 0.82);
      group.add(eyeDot);
    }
  }

  addArm(group, -1, shoulderWidth, shoulderY, hipY, height, limbRadius, accent);
  addArm(group, 1, shoulderWidth, shoulderY, hipY, height, limbRadius, accent);
  addLeg(group, -1, hipWidth, hipY, height, limbRadius * 1.12, skin);
  addLeg(group, 1, hipWidth, hipY, height, limbRadius * 1.12, skin);

  if (options.opacity > 0.7) {
    addHandMarkers(group, options.handPreference, shoulderWidth, hipY, height, marker);

    const badge = createBadge(options.bloodLabel || "-", options.bloodColor || "#a43d55");
    badge.position.set(0, chestY, torsoRadius * 0.74 + 0.045);
    badge.scale.set(0.42, 0.2, 1);
    group.add(badge);
  }

  const label = createTextSprite(options.label, options.opacity > 0.7 ? "#0f766e" : "#6e7b77", "#f8fbf9");
  label.position.set(0, height + 0.16, 0);
  label.scale.set(0.48, 0.18, 1);
  group.add(label);

  group.traverse((node) => {
    if (node.isMesh || node.isSprite) {
      node.castShadow = false;
      node.receiveShadow = false;
    }
  });

  return group;
}

function addArm(group, side, shoulderWidth, shoulderY, hipY, height, radius, mat) {
  const shoulder = new THREE.Vector3(side * shoulderWidth * 0.47, shoulderY - radius * 0.5, 0);
  const elbow = new THREE.Vector3(side * shoulderWidth * 0.64, height * 0.51, 0.03);
  const hand = new THREE.Vector3(side * shoulderWidth * 0.52, hipY - height * 0.06, 0.07);
  group.add(cylinderBetween(shoulder, elbow, radius, mat, 18));
  group.add(cylinderBetween(elbow, hand, radius * 0.92, mat, 18));

  const handMesh = new THREE.Mesh(new THREE.SphereGeometry(radius * 1.45, 16, 10), mat);
  handMesh.position.copy(hand);
  group.add(handMesh);
}

function addLeg(group, side, hipWidth, hipY, height, radius, mat) {
  const hip = new THREE.Vector3(side * hipWidth * 0.34, hipY - radius * 0.25, 0);
  const knee = new THREE.Vector3(side * hipWidth * 0.25, height * 0.22, 0.02);
  const ankle = new THREE.Vector3(side * hipWidth * 0.34, 0.09, 0.04);
  group.add(cylinderBetween(hip, knee, radius, mat, 18));
  group.add(cylinderBetween(knee, ankle, radius * 0.9, mat, 18));

  const foot = new THREE.Mesh(new THREE.BoxGeometry(radius * 2.8, radius * 0.9, radius * 4), mat);
  foot.position.set(side * hipWidth * 0.38, 0.035, 0.12);
  foot.rotation.y = side * 0.08;
  group.add(foot);
}

function addHandMarkers(group, handPreference, shoulderWidth, hipY, height, mat) {
  const sides = [];
  if (handPreference === "left" || handPreference === "ambidextrous") {
    sides.push(-1);
  }

  if (handPreference === "right" || handPreference === "ambidextrous") {
    sides.push(1);
  }

  for (const side of sides) {
    const marker = new THREE.Mesh(new THREE.TorusGeometry(height * 0.036, height * 0.006, 10, 28), mat);
    marker.position.set(side * shoulderWidth * 0.52, hipY - height * 0.06, 0.09);
    marker.rotation.x = Math.PI / 2;
    marker.userData.pulse = true;
    group.add(marker);
  }
}

function createGuideLayer({ averageVisualHeight, userVisualHeight, averageHeightCm, userHeightCm }) {
  const group = new THREE.Group();

  const floor = new THREE.GridHelper(4.4, 12, 0x8cb7af, 0xd2dedb);
  floor.material.transparent = true;
  floor.material.opacity = 0.32;
  group.add(floor);

  const axisMaterial = new THREE.LineBasicMaterial({ color: 0x7a8a86, transparent: true, opacity: 0.72 });
  const userMaterial = new THREE.LineBasicMaterial({ color: 0x0f766e, transparent: true, opacity: 0.82 });
  const averageMaterial = new THREE.LineBasicMaterial({ color: 0x91a09c, transparent: true, opacity: 0.78 });

  group.add(lineFromPoints([[-1.65, 0, -0.12], [-1.65, Math.max(userVisualHeight, averageVisualHeight) + 0.22, -0.12]], axisMaterial));
  group.add(lineFromPoints([[-1.76, 0, -0.12], [1.76, 0, -0.12]], axisMaterial));
  group.add(lineFromPoints([[-1.52, averageVisualHeight, -0.12], [1.52, averageVisualHeight, -0.12]], averageMaterial));
  group.add(lineFromPoints([[-1.52, userVisualHeight, -0.1], [1.52, userVisualHeight, -0.1]], userMaterial));

  for (const y of [averageVisualHeight, userVisualHeight]) {
    group.add(lineFromPoints([[-1.72, y, -0.12], [-1.56, y, -0.12]], axisMaterial));
  }

  const averageLabel = createTextSprite(`${formatCm(averageHeightCm)} ort.`, "#65736f", "#f8fbf9");
  averageLabel.position.set(-1.52, averageVisualHeight + 0.08, 0.02);
  averageLabel.scale.set(0.6, 0.18, 1);
  group.add(averageLabel);

  const userLabel = createTextSprite(formatCm(userHeightCm), "#0f766e", "#f8fbf9");
  userLabel.position.set(1.52, userVisualHeight + 0.08, 0.02);
  userLabel.scale.set(0.48, 0.18, 1);
  group.add(userLabel);

  return group;
}

function createBadge(label, color) {
  return createTextSprite(label, "#ffffff", color);
}

function createTextSprite(text, foreground, background) {
  const canvas = document.createElement("canvas");
  canvas.width = 256;
  canvas.height = 96;
  const ctx = canvas.getContext("2d");
  ctx.clearRect(0, 0, canvas.width, canvas.height);
  roundedRect(ctx, 12, 14, 232, 68, 28, background);
  ctx.font = "700 34px Arial, sans-serif";
  ctx.fillStyle = foreground;
  ctx.textAlign = "center";
  ctx.textBaseline = "middle";
  ctx.fillText(text, 128, 49, 206);

  const texture = new THREE.CanvasTexture(canvas);
  texture.colorSpace = THREE.SRGBColorSpace;
  const sprite = new THREE.Sprite(new THREE.SpriteMaterial({ map: texture, transparent: true }));
  sprite.userData.texture = texture;
  return sprite;
}

function roundedRect(ctx, x, y, width, height, radius, color) {
  ctx.beginPath();
  ctx.moveTo(x + radius, y);
  ctx.arcTo(x + width, y, x + width, y + height, radius);
  ctx.arcTo(x + width, y + height, x, y + height, radius);
  ctx.arcTo(x, y + height, x, y, radius);
  ctx.arcTo(x, y, x + width, y, radius);
  ctx.closePath();
  ctx.fillStyle = color;
  ctx.fill();
}

function cylinderBetween(start, end, radius, mat, segments) {
  const direction = new THREE.Vector3().subVectors(end, start);
  const length = direction.length();
  const geometry = new THREE.CylinderGeometry(radius, radius, length, segments, 1, true);
  const mesh = new THREE.Mesh(geometry, mat);
  mesh.position.copy(start).add(end).multiplyScalar(0.5);
  mesh.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), direction.normalize());
  return mesh;
}

function lineFromPoints(points, mat) {
  const geometry = new THREE.BufferGeometry().setFromPoints(points.map(([x, y, z]) => new THREE.Vector3(x, y, z)));
  return new THREE.Line(geometry, mat);
}

function material(color, opacity = 1, roughness = 0.65, metalness = 0.02) {
  return new THREE.MeshStandardMaterial({
    color,
    metalness,
    opacity,
    roughness,
    transparent: opacity < 1,
  });
}

function mapHeight(value, min, max) {
  return THREE.MathUtils.clamp(THREE.MathUtils.mapLinear(value, min, max, 1.78, 2.48), 1.68, 2.56);
}

function resize(state) {
  const rect = state.canvas.getBoundingClientRect();
  const width = Math.max(1, Math.floor(rect.width));
  const height = Math.max(1, Math.floor(rect.height));

  state.renderer.setSize(width, height, false);
  state.camera.aspect = width / height;
  state.camera.updateProjectionMatrix();
  state.canvas.dataset.pixelWidth = String(state.renderer.domElement.width);
  state.canvas.dataset.pixelHeight = String(state.renderer.domElement.height);
}

function animate(state) {
  state.animationFrame = requestAnimationFrame(() => animate(state));
  state.frameCount += 1;

  const clock = performance.now() * 0.001;
  const autoDrift = state.isDragging ? 0 : Math.sin(clock * 0.55) * 0.08;
  state.root.rotation.y += (state.rotationTarget + autoDrift - state.root.rotation.y) * 0.08;
  state.root.traverse((node) => {
    if (node.userData.pulse) {
      const pulse = 1 + Math.sin(clock * 3.1) * 0.08;
      node.scale.set(pulse, pulse, pulse);
    }
  });

  state.renderer.render(state.scene, state.camera);
  state.canvas.dataset.frameCount = String(state.frameCount);
  state.canvas.dataset.rotationY = state.root.rotation.y.toFixed(4);
}

function disposeObject(object) {
  object.traverse((node) => {
    if (node.geometry) {
      node.geometry.dispose();
    }

    if (node.material) {
      const materials = Array.isArray(node.material) ? node.material : [node.material];
      for (const item of materials) {
        if (item.map) {
          item.map.dispose();
        }
        item.dispose();
      }
    }

    if (node.userData.texture) {
      node.userData.texture.dispose();
    }
  });
}

function toNumber(value, fallback) {
  const number = Number(value);
  return Number.isFinite(number) ? number : fallback;
}

function formatCm(value) {
  return `${Math.round(value)} cm`;
}
