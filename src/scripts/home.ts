// The homepage owns the iframe lifecycle only. Unity's files and loading UI stay untouched.
const adventure = document.querySelector<HTMLElement>('#adventure');
const art = document.querySelector<HTMLElement>('#hero-art');
const play = document.querySelector<HTMLButtonElement>('#play-button');
const playLabel = document.querySelector<HTMLElement>('#play-label');
const panel = document.querySelector<HTMLElement>('#game-panel');
const viewport = document.querySelector<HTMLElement>('#game-viewport');
const mount = document.querySelector<HTMLElement>('#game-mount');
const loading = document.querySelector<HTMLElement>('#entry-loading');
const statusElement = document.querySelector<HTMLElement>('#game-status');
const retry = document.querySelector<HTMLButtonElement>('#retry-button');
const back = document.querySelector<HTMLButtonElement>('#back-button');
const canvas = document.querySelector<HTMLCanvasElement>('#stardust');

if (adventure && art && play && playLabel && panel && viewport && mount && loading && statusElement && retry && back && canvas) {
  const scene = adventure;
  const heroArt = art;
  const playButton = play;
  const label = playLabel;
  const gamePanel = panel;
  const gameViewport = viewport;
  const gameMount = mount;
  const entryLoading = loading;
  const gameStatus = statusElement;
  const retryButton = retry;
  const backButton = back;
  const particleCanvas = canvas;
  const ctx = particleCanvas.getContext('2d');
  const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
  const finePointer = window.matchMedia('(hover: hover) and (pointer: fine)');
  const narrowScreen = window.matchMedia('(max-width: 640px)');
  const characterImages = Array.from(heroArt.querySelectorAll<HTMLImageElement>('[data-idle-src]'));
  for (const image of characterImages) {
    image.addEventListener('error', () => {
      if (image.getAttribute('src') !== image.dataset.idleSrc) return;
      image.dataset.idleFailed = 'true';
      image.dataset.characterState = 'fallback';
      if (image.dataset.posterSrc) image.src = image.dataset.posterSrc;
    });
  }

  type State = 'home' | 'leaving' | 'loading' | 'game';
  type Particle = { x: number; y: number; size: number; speed: number; phase: number };
  let state: State = 'home';
  let inView = true;
  let raf = 0;
  let lastFrame = 0;
  let width = 0;
  let height = 0;
  let particles: Particle[] = [];
  let pointerX = 0;
  let pointerY = 0;
  let currentX = 0;
  let currentY = 0;
  let frame: HTMLIFrameElement | null = null;
  let generation = 0;
  let transitionTimer: ReturnType<typeof setTimeout> | undefined;
  let entryTimer: ReturnType<typeof setTimeout> | undefined;

  function motionEnabled() { return !reduceMotion.matches; }
  function shouldAnimate() { return motionEnabled() && state === 'home' && inView && !document.hidden; }

  function resizeParticles() {
    const rect = heroArt.getBoundingClientRect();
    width = rect.width;
    height = rect.height;
    const ratio = Math.min(window.devicePixelRatio || 1, 1.5);
    particleCanvas.width = Math.round(width * ratio);
    particleCanvas.height = Math.round(height * ratio);
    ctx?.setTransform(ratio, 0, 0, ratio, 0, 0);
    const count = narrowScreen.matches ? 15 : 40;
    particles = Array.from({ length: count }, () => ({ x: Math.random() * width, y: Math.random() * height * .82, size: Math.random() * 1.5 + .5, speed: Math.random() * 7 + 3, phase: Math.random() * Math.PI * 2 }));
    if (!finePointer.matches) resetPointer();
  }

  function resetPointer() {
    pointerX = pointerY = currentX = currentY = 0;
    scene.style.setProperty('--pointer-x', '0');
    scene.style.setProperty('--pointer-y', '0');
  }

  function animate(time: number) {
    raf = 0;
    if (!shouldAnimate()) return;
    const delta = lastFrame ? Math.min((time - lastFrame) / 1000, .05) : 0;
    lastFrame = time;
    if (finePointer.matches) {
      currentX += (pointerX - currentX) * .055;
      currentY += (pointerY - currentY) * .055;
      scene.style.setProperty('--pointer-x', currentX.toFixed(4));
      scene.style.setProperty('--pointer-y', currentY.toFixed(4));
    }
    if (ctx) {
      ctx.clearRect(0, 0, width, height);
      for (const particle of particles) {
        particle.y -= particle.speed * delta;
        particle.x += Math.sin(time / 4500 + particle.phase) * delta * 3;
        if (particle.y < -4) { particle.y = height * .82; particle.x = Math.random() * width; }
        const opacity = (.35 + Math.sin(time / 1600 + particle.phase) * .2) * (particle.y < height * .38 ? 1 : .45);
        ctx.fillStyle = `rgba(255, 220, 211, ${opacity})`;
        ctx.beginPath();
        ctx.arc(particle.x, particle.y, particle.size, 0, Math.PI * 2);
        ctx.fill();
      }
    }
    raf = requestAnimationFrame(animate);
  }

  function syncAnimation() {
    const enabled = motionEnabled();
    const active = shouldAnimate();
    scene.dataset.motion = enabled ? 'on' : 'off';
    scene.dataset.animating = String(active);
    // GIFs cannot be paused with animation-play-state. Replace them with their
    // actual first frames while inactive; resume the same six-frame assets on return.
    for (const image of characterImages) {
      const playing = active && image.dataset.idleFailed !== 'true';
      const source = playing ? image.dataset.idleSrc : image.dataset.posterSrc;
      image.dataset.characterState = image.dataset.idleFailed === 'true' ? 'fallback' : playing ? 'playing' : 'paused';
      if (source && image.getAttribute('src') !== source) image.src = source;
    }
    if (active && !raf) { lastFrame = 0; raf = requestAnimationFrame(animate); }
    if (!active) {
      cancelAnimationFrame(raf);
      raf = 0;
      lastFrame = 0;
      resetPointer();
      ctx?.clearRect(0, 0, width, height);
    }
  }

  function setState(next: State) {
    state = next;
    scene.dataset.state = state;
    syncAnimation();
  }

  function clearEntry() {
    generation += 1;
    clearTimeout(transitionTimer);
    clearTimeout(entryTimer);
    if (frame) { frame.onload = null; frame.onerror = null; frame.remove(); frame = null; }
    gameMount.replaceChildren();
    gameViewport.setAttribute('aria-busy', 'false');
  }

  function entryFailed(message: string) {
    entryLoading.hidden = true;
    gameViewport.setAttribute('aria-busy', 'false');
    gameStatus.textContent = message;
    retryButton.hidden = false;
  }

  async function openGame() {
    clearEntry();
    const token = generation;
    setState('loading');
    heroArt.inert = true;
    gamePanel.hidden = false;
    entryLoading.hidden = false;
    gameViewport.setAttribute('aria-busy', 'true');
    retryButton.hidden = true;
    gameStatus.textContent = 'Opening the game window. The game will show its own loading progress.';
    backButton.focus({ preventScroll: true });

    entryTimer = setTimeout(() => {
      if (token === generation && state === 'loading') entryFailed('This is taking longer than expected. You can keep waiting or try again.');
    }, 30_000);

    // Check the entry response to distinguish an HTTP/network error from iframe load.
    // This starts only after Play Now and does not fetch the Unity build binaries.
    try {
      const response = await fetch('/game/index.html', { method: 'HEAD', cache: 'no-cache' });
      if (token !== generation) return;
      if (!response.ok) {
        clearTimeout(entryTimer);
        entryFailed('The game window could not be opened. Please try again.');
        return;
      }
    } catch {
      if (token !== generation) return;
      clearTimeout(entryTimer);
      entryFailed('The connection to the game was interrupted. Please try again.');
      return;
    }
    if (token !== generation) return;
    const nextFrame = document.createElement('iframe');
    nextFrame.className = 'game-frame';
    nextFrame.title = 'The Fright Before Christmas WebGL Player';
    nextFrame.allow = 'autoplay; fullscreen; gamepad';
    nextFrame.allowFullscreen = true;
    nextFrame.onload = () => {
      if (token !== generation || frame !== nextFrame) return;
      clearTimeout(entryTimer);
      entryLoading.hidden = true;
      gameViewport.setAttribute('aria-busy', 'false');
      retryButton.hidden = true;
      // This means the shell loaded, NOT that Unity finished initializing.
      gameStatus.textContent = 'Follow the loading progress inside the game window. Click inside it to play.';
      setState('game');
    };
    nextFrame.onerror = () => {
      if (token !== generation) return;
      clearTimeout(entryTimer);
      entryFailed('The game window could not be opened. Please try again.');
    };
    frame = nextFrame;
    nextFrame.src = '/game/index.html';
    gameMount.append(nextFrame);
  }

  playButton.addEventListener('click', () => {
    if (state !== 'home') return;
    playButton.disabled = true;
    label.textContent = 'Opening…';
    setState('leaving');
    transitionTimer = setTimeout(openGame, motionEnabled() ? 400 : 0);
  });
  retryButton.addEventListener('click', () => { void openGame(); });
  backButton.addEventListener('click', () => {
    clearEntry();
    gamePanel.hidden = true;
    heroArt.inert = false;
    entryLoading.hidden = true;
    gameStatus.textContent = '';
    label.textContent = 'Play now';
    playButton.disabled = false;
    setState('home');
    playButton.focus({ preventScroll: true });
  });
  heroArt.addEventListener('pointermove', (event: PointerEvent) => {
    if (!shouldAnimate() || !finePointer.matches || event.pointerType === 'touch') return;
    const rect = heroArt.getBoundingClientRect();
    pointerX = Math.max(-1, Math.min(1, ((event.clientX - rect.left) / rect.width - .5) * 2));
    pointerY = Math.max(-1, Math.min(1, ((event.clientY - rect.top) / rect.height - .5) * 2));
  }, { passive: true });
  heroArt.addEventListener('pointerleave', () => { pointerX = pointerY = 0; });
  document.addEventListener('visibilitychange', syncAnimation);
  reduceMotion.addEventListener('change', syncAnimation);
  finePointer.addEventListener('change', resetPointer);
  const observer = new IntersectionObserver(([entry]) => { inView = entry.isIntersecting; syncAnimation(); }, { threshold: .05 });
  observer.observe(scene);
  const resizeObserver = new ResizeObserver(resizeParticles);
  resizeObserver.observe(heroArt);
  const logo = document.querySelector<HTMLImageElement>('#game-logo');
  function showLogoFallback() {
    if (!logo) return;
    logo.hidden = true;
    const fallback = document.querySelector<HTMLElement>('.logo-fallback');
    if (fallback) fallback.hidden = false;
  }
  logo?.addEventListener('error', showLogoFallback);
  if (logo?.complete && !logo.naturalWidth) showLogoFallback();
  window.addEventListener('pagehide', () => { clearEntry(); cancelAnimationFrame(raf); raf = 0; });
  window.addEventListener('pageshow', (event) => {
    if (event.persisted) backButton.click();
  });
  playButton.disabled = false;
  resizeParticles();
  syncAnimation();
}
