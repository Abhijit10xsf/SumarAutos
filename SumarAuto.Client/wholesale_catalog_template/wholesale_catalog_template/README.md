# TradeParts Wholesale Catalog Template

A responsive wholesale/B2B automotive-parts catalog inspired by the supplied reference screenshot.

## Included

- Responsive desktop and mobile navigation
- Search by part/OE number, vehicle, VIN, and OEM list
- Category, brand, warehouse, stock, offer, and price filters
- Grid/list view switch
- Product stock by warehouse and in-transit quantity
- MOQ-aware quantity controls
- Working client-side cart with VAT calculation
- Mobile off-canvas filters and cart
- Six editable SVG product placeholders

## Free components

- Bootstrap 5 (MIT License)
- Bootstrap 5.1.1 is bundled locally (MIT License)
- The small icon set is included as CSS/Unicode, so the demo works offline

## Run

The template is fully offline. Open `index.html` directly, or run a local web server:

```bash
python -m http.server 8000
```

Then open `http://localhost:8000`.

## Backend integration

Replace the `products` array in `assets/js/app.js` with data from your API, SAP Business One Service Layer, or MVC controller. The cart and filter logic are intentionally separated into small functions for easy integration.
