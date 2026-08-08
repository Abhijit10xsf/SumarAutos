const products = [
  {
    id: 1,
    code: "MCP-001",
    title: "Seal ring, spark plug tube",
    brand: "FEBEST",
    category: "Engine",
    image: "assets/img/seal-ring.svg",
    ean: "4056111015293",
    oe: "10966-AA000",
    compatibility: "Subaru / Chery / Mitsubishi",
    specs: { "Inner diameter": "24 mm", "Outer diameter": "35 mm" },
    sharjah: 119,
    jebel: 2368,
    transit: 180,
    price: 6.06,
    moq: 1,
    offer: true
  },
  {
    id: 2,
    code: "AF-4198",
    title: "Premium engine air filter",
    brand: "BOSCH",
    category: "Filters",
    image: "assets/img/air-filter.svg",
    ean: "4047026500198",
    oe: "17801-0M030",
    compatibility: "Toyota Corolla / Yaris",
    specs: { "Length": "278 mm", "Width": "167 mm" },
    sharjah: 320,
    jebel: 985,
    transit: 80,
    price: 21.75,
    moq: 2,
    offer: false
  },
  {
    id: 3,
    code: "BP-7740",
    title: "Ceramic front brake pad set",
    brand: "FEBEST",
    category: "Brake",
    image: "assets/img/brake-pad.svg",
    ean: "4056111097749",
    oe: "D1060-JA00A",
    compatibility: "Nissan Altima / Teana",
    specs: { "Axle": "Front", "Wear sensor": "Included" },
    sharjah: 86,
    jebel: 412,
    transit: 120,
    price: 68.50,
    moq: 1,
    offer: true
  },
  {
    id: 4,
    code: "SP-7164",
    title: "Iridium spark plug long life",
    brand: "NGK",
    category: "Electrical",
    image: "assets/img/spark-plug.svg",
    ean: "0087295171646",
    oe: "ILZKR7B11",
    compatibility: "Honda / Hyundai / Kia",
    specs: { "Thread": "M12 x 1.25", "Gap": "1.1 mm" },
    sharjah: 560,
    jebel: 1900,
    transit: 0,
    price: 31.20,
    moq: 4,
    offer: false
  },
  {
    id: 5,
    code: "WB-2305",
    title: "Front wheel bearing kit",
    brand: "SKF",
    category: "Suspension",
    image: "assets/img/bearing.svg",
    ean: "7316575623052",
    oe: "40210-2Y000",
    compatibility: "Nissan Maxima / Murano",
    specs: { "Inner diameter": "45 mm", "Outer diameter": "84 mm" },
    sharjah: 45,
    jebel: 98,
    transit: 60,
    price: 145.90,
    moq: 1,
    offer: false
  },
  {
    id: 6,
    code: "DB-6PK2135",
    title: "Multi-rib auxiliary drive belt",
    brand: "BOSCH",
    category: "Engine",
    image: "assets/img/drive-belt.svg",
    ean: "4047025272133",
    oe: "6PK2135",
    compatibility: "Toyota / Lexus / Mitsubishi",
    specs: { "Ribs": "6", "Length": "2135 mm" },
    sharjah: 210,
    jebel: 640,
    transit: 150,
    price: 47.80,
    moq: 2,
    offer: true
  }
];

let cart = [];
let listMode = false;

const productGrid = document.getElementById("productGrid");
const resultCount = document.getElementById("resultCount");
const emptyState = document.getElementById("emptyState");
const cartToast = new bootstrap.Toast(document.getElementById("cartToast"));

function money(value) {
  return `AED ${value.toFixed(2)}`;
}

function selectedBrands() {
  return [...document.querySelectorAll(".brand-filter:checked")].map(x => x.value);
}

function getFilteredProducts() {
  const query = document.getElementById("catalogSearch").value.trim().toLowerCase();
  const category = document.getElementById("categoryFilter").value;
  const brands = selectedBrands();
  const minPrice = Number(document.getElementById("minPrice").value || 0);
  const maxPrice = Number(document.getElementById("maxPrice").value || Number.MAX_SAFE_INTEGER);
  const inStockOnly = document.getElementById("inStockOnly").checked;
  const offersOnly = document.getElementById("offersOnly").checked;
  const sharjahOnly = document.getElementById("sharjahOnly").checked;
  const jebelOnly = document.getElementById("jebelOnly").checked;

  let result = products.filter(p => {
    const searchable = `${p.code} ${p.title} ${p.brand} ${p.ean} ${p.oe} ${p.compatibility}`.toLowerCase();
    return (!query || searchable.includes(query)) &&
      (category === "all" || p.category === category) &&
      (!brands.length || brands.includes(p.brand)) &&
      p.price >= minPrice && p.price <= maxPrice &&
      (!inStockOnly || (p.sharjah + p.jebel) > 0) &&
      (!offersOnly || p.offer) &&
      (!sharjahOnly || p.sharjah > 0) &&
      (!jebelOnly || p.jebel > 0);
  });

  const sort = document.getElementById("sortSelect").value;
  if (sort === "price-low") result.sort((a, b) => a.price - b.price);
  if (sort === "price-high") result.sort((a, b) => b.price - a.price);
  if (sort === "stock") result.sort((a, b) => (b.sharjah + b.jebel) - (a.sharjah + a.jebel));
  return result;
}

function productTemplate(product) {
  const specRows = Object.entries(product.specs)
    .map(([label, value]) => `<div><dt>${label}</dt><dd>${value}</dd></div>`)
    .join("");

  return `
    <div class="product-col col-md-6 col-xxl-4 ${listMode ? "list-mode" : ""}" data-product-id="${product.id}">
      <article class="product-card">
        <div class="product-image-wrap">
          ${product.offer ? '<span class="offer-badge badge text-bg-danger">Special offer</span>' : ""}
          <button class="wishlist-btn" type="button" aria-label="Add to favorites"><i class="bi bi-heart"></i></button>
          <img class="product-image" src="${product.image}" alt="${product.title}">
        </div>
        <div class="product-body">
          <div class="product-main">
            <div class="product-brand">${product.brand}</div>
            <h2 class="product-title">${product.code} — ${product.title}</h2>
            <div class="part-number">OE: ${product.oe} · EAN: ${product.ean}</div>

            <div class="stock-row">
              <span class="stock-chip"><i class="bi bi-box-seam"></i><strong>${product.sharjah}</strong> Sharjah</span>
              <span class="stock-chip"><i class="bi bi-box-seam"></i><strong>${product.jebel}</strong> Jebel Ali</span>
              <span class="stock-chip"><i class="bi bi-truck"></i><strong>${product.transit}</strong> Transit</span>
            </div>

            <dl class="product-specs">
              ${specRows}
              <div><dt>Compatibility</dt><dd>${product.compatibility}</dd></div>
            </dl>
          </div>

          <div class="product-purchase">
            <div class="price-line">
              <div class="price">${money(product.price)} <small>/ item</small></div>
              <div class="moq">MOQ<br><strong>${product.moq}</strong></div>
            </div>

            <div class="quantity-control">
              <button class="qty-minus" type="button" aria-label="Decrease quantity">−</button>
              <input class="qty-input" type="number" min="${product.moq}" step="${product.moq}" value="${product.moq}" aria-label="Quantity">
              <button class="qty-plus" type="button" aria-label="Increase quantity">+</button>
            </div>
            <button class="btn btn-danger w-100 add-cart-btn" type="button"><i class="bi bi-cart-plus me-1"></i> Add to Cart</button>
          </div>
        </div>
      </article>
    </div>`;
}

function renderProducts() {
  const filtered = getFilteredProducts();
  resultCount.textContent = filtered.length;
  productGrid.innerHTML = filtered.map(productTemplate).join("");
  emptyState.classList.toggle("d-none", filtered.length > 0);
  bindProductEvents();
}

function bindProductEvents() {
  document.querySelectorAll(".product-col").forEach(card => {
    const product = products.find(p => p.id === Number(card.dataset.productId));
    const input = card.querySelector(".qty-input");

    card.querySelector(".qty-minus").addEventListener("click", () => {
      input.value = Math.max(product.moq, Number(input.value || product.moq) - product.moq);
    });
    card.querySelector(".qty-plus").addEventListener("click", () => {
      input.value = Number(input.value || product.moq) + product.moq;
    });
    card.querySelector(".add-cart-btn").addEventListener("click", () => {
      addToCart(product.id, Number(input.value || product.moq));
    });
    card.querySelector(".wishlist-btn").addEventListener("click", event => {
      const icon = event.currentTarget.querySelector("i");
      icon.classList.toggle("bi-heart");
      icon.classList.toggle("bi-heart-fill");
      event.currentTarget.classList.toggle("text-danger");
    });
  });
}

function addToCart(productId, quantity) {
  const product = products.find(p => p.id === productId);
  const current = cart.find(item => item.id === productId);
  if (current) current.quantity += quantity;
  else cart.push({ id: productId, quantity });
  updateCart();
  cartToast.show();
}

function removeFromCart(productId) {
  cart = cart.filter(item => item.id !== productId);
  updateCart();
}

function updateCart() {
  const cartCount = cart.reduce((sum, item) => sum + item.quantity, 0);
  document.getElementById("cartCount").textContent = cartCount;

  const cartItems = document.getElementById("cartItems");
  if (!cart.length) {
    cartItems.innerHTML = `<div class="cart-empty"><div><i class="bi bi-cart3"></i><h6 class="mt-3">Your cart is empty</h6><p class="small mb-0">Add catalog items to begin an order.</p></div></div>`;
  } else {
    cartItems.innerHTML = cart.map(item => {
      const p = products.find(product => product.id === item.id);
      return `<div class="cart-item">
        <img src="${p.image}" alt="${p.title}">
        <div>
          <div class="cart-item-title">${p.code} — ${p.title}</div>
          <div class="cart-item-meta">${item.quantity} × ${money(p.price)}</div>
          <strong class="small">${money(item.quantity * p.price)}</strong>
        </div>
        <button class="cart-remove" onclick="removeFromCart(${p.id})" aria-label="Remove item"><i class="bi bi-trash"></i></button>
      </div>`;
    }).join("");
  }

  const subtotal = cart.reduce((sum, item) => {
    const p = products.find(product => product.id === item.id);
    return sum + p.price * item.quantity;
  }, 0);
  const vat = subtotal * .05;
  document.getElementById("cartSubtotal").textContent = money(subtotal);
  document.getElementById("cartVat").textContent = money(vat);
  document.getElementById("cartTotal").textContent = money(subtotal + vat);
}

function bindFilters() {
  [
    "catalogSearch", "categoryFilter", "minPrice", "maxPrice", "inStockOnly",
    "offersOnly", "sharjahOnly", "jebelOnly", "sortSelect"
  ].forEach(id => {
    const el = document.getElementById(id);
    el.addEventListener(el.type === "checkbox" || el.tagName === "SELECT" ? "change" : "input", renderProducts);
  });
  document.querySelectorAll(".brand-filter").forEach(el => el.addEventListener("change", renderProducts));
  document.getElementById("searchButton").addEventListener("click", renderProducts);

  document.getElementById("clearFilters").addEventListener("click", () => {
    document.getElementById("catalogSearch").value = "";
    document.getElementById("categoryFilter").value = "all";
    document.getElementById("minPrice").value = "";
    document.getElementById("maxPrice").value = "";
    ["inStockOnly", "offersOnly", "sharjahOnly", "jebelOnly"].forEach(id => document.getElementById(id).checked = false);
    document.querySelectorAll(".brand-filter").forEach(el => el.checked = false);
    document.getElementById("sortSelect").value = "featured";
    renderProducts();
  });

  document.getElementById("gridViewBtn").addEventListener("click", () => {
    listMode = false;
    document.getElementById("gridViewBtn").classList.add("active");
    document.getElementById("listViewBtn").classList.remove("active");
    renderProducts();
  });
  document.getElementById("listViewBtn").addEventListener("click", () => {
    listMode = true;
    document.getElementById("listViewBtn").classList.add("active");
    document.getElementById("gridViewBtn").classList.remove("active");
    renderProducts();
  });
}

window.removeFromCart = removeFromCart;
bindFilters();
renderProducts();
updateCart();
