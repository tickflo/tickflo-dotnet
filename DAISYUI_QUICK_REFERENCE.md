# DaisyUI Quick Reference Guide for Tickflo

## Button Styling Standards

### Primary Actions (Save, Submit, Confirm)
```html
<button class="btn btn-primary">Save</button>
<a class="btn btn-primary" href="/path">Continue</a>
```

### Secondary Actions (Edit, View, Filter)
```html
<button class="btn btn-soft">Edit</button>
<a class="btn btn-soft" href="/path">View</a>
```

### Tertiary Actions (Cancel, Back, Close)
```html
<button class="btn btn-ghost">Cancel</button>
<a class="btn btn-ghost" href="/path">Back</a>
```

### Dangerous Actions (Delete, Remove, Reject)
```html
<button class="btn btn-error">Delete</button>
<button class="btn btn-error btn-outline">Permanently Delete</button>
```

### Disabled State
```html
<button class="btn btn-primary" disabled>Disabled</button>
```

---

## Button Size & Modifiers

### Sizes
```html
<button class="btn btn-xs">Extra Small</button>
<button class="btn btn-sm">Small</button>
<button class="btn btn-md">Medium (default)</button>
<button class="btn btn-lg">Large</button>
<button class="btn btn-xl">Extra Large</button>
```

### Responsive Sizing
```html
<button class="btn btn-sm md:btn-md">Small on mobile, Medium on desktop</button>
```

### Modifiers
```html
<button class="btn btn-wide">Wide button (full width)</button>
<button class="btn btn-block">Block button (100% width)</button>
<button class="btn btn-square">Square button</button>
<button class="btn btn-circle">Circle button</button>
```

### Join Group
```html
<div class="join">
  <a class="btn join-item">First</a>
  <a class="btn join-item">Second</a>
  <a class="btn join-item">Third</a>
</div>
```

---

## Link Styling Standards

### Basic Link with Hover
```html
<a class="link link-hover">Click me</a>
```

### Link with Semantic Color (REQUIRED)
```html
<a class="link link-primary link-hover">Primary Link</a>
<a class="link link-secondary link-hover">Secondary Link</a>
<a class="link link-error link-hover">Error Link</a>
<a class="link link-warning link-hover">Warning Link</a>
```

---

## Badge Styling

### Standard Badges
```html
<span class="badge">Default</span>
<span class="badge badge-neutral">Neutral</span>
<span class="badge badge-primary">Primary</span>
<span class="badge badge-secondary">Secondary</span>
<span class="badge badge-error">Error</span>
<span class="badge badge-warning">Warning</span>
<span class="badge badge-success">Success</span>
<span class="badge badge-info">Info</span>
```

### Badge Styles
```html
<span class="badge badge-soft">Soft (default)</span>
<span class="badge badge-outline">Outline</span>
<span class="badge badge-dash">Dashed</span>
<span class="badge badge-ghost">Ghost</span>
```

### Badge Sizes
```html
<span class="badge badge-xs">Extra Small</span>
<span class="badge badge-sm">Small</span>
<span class="badge badge-md">Medium (default)</span>
<span class="badge badge-lg">Large</span>
```

---

## Alert Styling

### Alert Types
```html
<div role="alert" class="alert alert-info">Information message</div>
<div role="alert" class="alert alert-success">Success message</div>
<div role="alert" class="alert alert-warning">Warning message</div>
<div role="alert" class="alert alert-error">Error message</div>
```

### Alert Styles (Use alert-soft for consistency)
```html
<div role="alert" class="alert alert-soft alert-info">Soft style (preferred)</div>
<div role="alert" class="alert alert-outline alert-info">Outline style</div>
<div role="alert" class="alert alert-dash alert-info">Dashed style</div>
```

---

## Form Control Standards

### Text Input
```html
<input type="text" placeholder="Type here..." class="input input-bordered w-full" />
```

### Select/Dropdown
```html
<select class="select select-bordered w-full">
  <option>Option 1</option>
  <option>Option 2</option>
</select>
```

### Textarea
```html
<textarea class="textarea textarea-bordered w-full" placeholder="Enter text..."></textarea>
```

### Fieldset (for form grouping)
```html
<fieldset class="fieldset">
  <label class="label">
    <span class="label-text">Email</span>
  </label>
  <input type="email" class="input input-bordered w-full" />
</fieldset>
```

### Checkbox
```html
<input type="checkbox" class="checkbox" />
<input type="checkbox" class="checkbox checkbox-primary" />
<input type="checkbox" class="checkbox checkbox-error" />
```

### Radio
```html
<input type="radio" name="option" class="radio" />
<input type="radio" name="option" class="radio radio-primary" />
```

### Toggle Switch
```html
<input type="checkbox" class="toggle" />
<input type="checkbox" class="toggle toggle-primary" />
```

---

## Card Components

### Basic Card
```html
<div class="card bg-base-100 shadow-md">
  <div class="card-body">
    <h2 class="card-title">Title</h2>
    <p>Content goes here</p>
  </div>
</div>
```

### Card with Image
```html
<div class="card bg-base-100 shadow-md">
  <figure>
    <img src="/image.jpg" alt="Card image" />
  </figure>
  <div class="card-body">
    <h2 class="card-title">Title</h2>
    <p>Content</p>
  </div>
</div>
```

### Card with Actions
```html
<div class="card bg-base-100">
  <div class="card-body">
    <h2 class="card-title">Title</h2>
    <div class="card-actions">
      <button class="btn btn-primary">Action</button>
      <button class="btn btn-ghost">Cancel</button>
    </div>
  </div>
</div>
```

---

## Table Styling

### Basic Table with Zebra Stripes
```html
<div class="overflow-x-auto">
  <table class="table table-zebra table-md">
    <thead>
      <tr>
        <th>Column 1</th>
        <th>Column 2</th>
        <th>Action</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td>Data 1</td>
        <td>Data 2</td>
        <td><button class="btn btn-ghost btn-sm">Edit</button></td>
      </tr>
    </tbody>
  </table>
</div>
```

### Table Sizes
```html
<table class="table table-xs">...</table>
<table class="table table-sm">...</table>
<table class="table table-md">...</table>
<table class="table table-lg">...</table>
```

---

## Colors & Semantic Color Names

### Primary Colors (Use these consistently)
```html
<!-- Text -->
<span class="text-primary">Primary text</span>
<span class="text-secondary">Secondary text</span>
<span class="text-accent">Accent text</span>
<span class="text-neutral">Neutral text</span>

<!-- Background -->
<div class="bg-primary">Primary background</div>
<div class="bg-base-100">Base background (page)</div>
<div class="bg-base-200">Elevated background</div>
<div class="bg-base-300">Higher elevation</div>

<!-- Status Colors -->
<span class="text-success">Success</span>
<span class="text-warning">Warning</span>
<span class="text-error">Error</span>
<span class="text-info">Info</span>
```

### DO NOT USE
❌ `text-gray-800`, `bg-red-500`, `text-blue-200`  
✅ USE instead: Semantic colors like `text-base-content`, `bg-error`, `text-primary`

---

## Responsive Design

### Responsive Classes
```html
<!-- Hidden on mobile, visible on medium screens and up -->
<div class="hidden md:block">Desktop content</div>

<!-- Different button size on mobile vs desktop -->
<button class="btn btn-sm md:btn-md">Mobile small, Desktop medium</button>

<!-- Different flex direction based on screen -->
<div class="flex flex-col md:flex-row">
  <!-- Column on mobile, row on desktop -->
</div>

<!-- Responsive grid -->
<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
  <!-- 1 column on mobile, 2 on tablet, 3 on desktop -->
</div>
```

### Breakpoints
- `sm:` - 640px
- `md:` - 768px
- `lg:` - 1024px
- `xl:` - 1280px
- `2xl:` - 1536px

---

## ANTI-PATTERNS (DO NOT USE)

### ❌ Multiple Button Styles Together
```html
<!-- WRONG -->
<button class="btn btn-soft btn-ghost">Wrong</button>
<button class="btn btn-outline btn-soft">Wrong</button>

<!-- RIGHT -->
<button class="btn btn-ghost">Right</button>
<button class="btn btn-soft">Right</button>
```

### ❌ Hardcoded Tailwind Colors
```html
<!-- WRONG -->
<span class="text-red-500">Status</span>
<div class="bg-blue-200">Card</div>

<!-- RIGHT -->
<span class="text-error">Status</span>
<div class="bg-primary">Card</div>
```

### ❌ Links Without Color
```html
<!-- WRONG -->
<a class="link link-hover">Click me</a>

<!-- RIGHT -->
<a class="link link-primary link-hover">Click me</a>
```

### ❌ Custom CSS for Component Styling
```css
/* WRONG */
.my-button {
  background: blue;
  padding: 10px;
  border-radius: 4px;
}

/* RIGHT */
Use DaisyUI components with btn, btn-primary, etc.
```

---

## Theme Switching

The application supports dark and light modes automatically:
- Dark theme is default
- User preference is saved in a cookie
- All colors automatically adapt to the selected theme

Users can toggle theme via the theme controller in the header.

---

## Spacing Standards

### Margin & Padding
```html
<div class="p-2">Small padding</div>
<div class="p-4">Medium padding (default)</div>
<div class="p-6">Large padding</div>
<div class="p-8">Extra large padding</div>

<div class="gap-2">Small gap</div>
<div class="gap-4">Medium gap</div>
<div class="gap-6">Large gap</div>
```

---

## Common Patterns

### Form with Submit and Cancel
```html
<form method="post" class="grid gap-4">
  <input type="text" class="input input-bordered" />
  <div class="flex gap-2">
    <button type="submit" class="btn btn-primary">Save</button>
    <a href="/back" class="btn btn-ghost">Cancel</a>
  </div>
</form>
```

### Action Buttons in List Item
```html
<div class="flex flex-wrap gap-2 md:join">
  <a class="btn btn-ghost btn-sm md:join-item">Edit</a>
  <a class="btn btn-primary btn-sm md:join-item">View</a>
  <button class="btn btn-error btn-sm md:join-item">Delete</button>
</div>
```

### Status Badge Display
```html
<div class="flex gap-2">
  <span class="badge badge-soft badge-success">Active</span>
  <span class="badge badge-soft badge-warning">Pending</span>
  <span class="badge badge-soft badge-error">Inactive</span>
</div>
```

### Card with Header Actions
```html
<div class="card bg-base-100">
  <div class="card-body">
    <div class="flex justify-between items-center">
      <h2 class="card-title">Title</h2>
      <button class="btn btn-ghost btn-sm">More</button>
    </div>
    <p>Content</p>
  </div>
</div>
```

---

## Need Help?

- **DaisyUI Docs:** https://daisyui.com/docs/
- **DaisyUI Components:** https://daisyui.com/components/
- **Tailwind CSS Utilities:** https://tailwindcss.com/docs/
- **Project Audit Report:** See `DAISYUI_AUDIT_REPORT.md`

---

**Last Updated:** January 3, 2026  
**Maintainer:** Development Team  
**Status:** Active & Current
