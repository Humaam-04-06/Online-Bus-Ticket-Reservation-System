# SRCTravel — Modern Online Bus Ticket Reservation System

## 🚀 Current Project Status: **Full System Audit & Business Logic Hardened**
The system has completed a comprehensive end-to-end security and logic audit. All three portals (Admin, Employee, Customer) have been verified for correctness. Input validation, seat integrity, voucher thresholds, and capacity enforcement are all active and tested. The build compiles with **0 errors and 0 warnings**.

---

## 🎨 UI / UX Design System (Premium Experience)
- **Theme**: Urban Nocturne (Premium Dark Mode)
- **Design Philosophy**: Glassmorphism, blurred panels, and neon glow accents.
- **Animations**:
    - **GSAP Stacking**: Section-on-section stacking effect on the landing page.
    - **Scroll Scaling**: Interactive section scaling (0.6 → 1.0) as users explore.
    - **Micro-interactions**: Hover effects, smooth transitions, and loading skeletons.
- **Responsiveness**: Mobile-first architecture using Tailwind CSS and targeted Media Queries.

---

## ✅ Core Features & Completed Modules

### 1. 📱 Universal Responsiveness
- **Adaptive Dashboards**: Grid systems for Admin, Employee, and Customer portals automatically reconfigure (4-col → 2-col → 1-col).
- **Mobile Navigation**: Slide-out sidebars with glassmorphic overlays for portal management on the go.
- **Touch-Friendly Tables**: Horizontal scroll wrappers for all data tables, ensuring usability on small viewports.
- **Responsive Auth**: Sliding login/register panels stack vertically on mobile with dedicated toggle switches.

### 2. 🔐 Authentication & Security
- **Dynamic Auth UX**: Navbar "Sign Up" button intelligently routes to the registration panel via `?register=true` query parameters.
- **Security**: BCrypt hashing, CSRF protection, and role-based access control (`Admin`, `Employee`, `Customer`).
- **Validation**: Real-time password strength meter with visual feedback and submission blocking.
- **Google OAuth**: Customers may sign in via Google. Password change is blocked for OAuth-only accounts.

### 3. 🏢 Admin Portal (The Command Center)
- **Fleet Management**: Configure and track various bus classes (Standard, Luxury, etc.) with duplicate-number protection.
- **Operations Control**: Manage Routes, Prices, and detailed Bus Schedules.
- **Personnel Management**: Full CRUD and blocking capabilities for Employee accounts with last-admin lockout prevention.
- **Cascade-Safe Deletes**: Routes/schedules with active bookings cannot be deleted — only deactivated.
- **Global Insights**: High-level stats, revenue reports, and booking trends.

### 4. 🛠️ Employee Portal (Field Operations)
- **Request Processing**: Accept or Reject customer booking requests with real-time feedback.
- **Seat Integrity**: Double-booking is blocked — the system checks `Pending`, `Accepted`, and `Completed` requests before allowing any seat assignment.
- **Capacity Enforcement**: Assigned seat count is validated against the bus's physical seating capacity.
- **Voucher Re-validation**: Minimum fare thresholds on vouchers are re-checked at acceptance time — not just at submission.
- **Ticketing**: Instant HTML ticket generation for confirmed passengers.
- **Active Tracking**: Dashboard for today's departures and pending tasks.

### 5. 👤 Customer Portal (Traveler Experience)
- **Smart Booking**: 60/40 split-screen booking form with auto-fill from public search.
- **Date Guard**: Past travel dates are rejected server-side.
- **Seat Count Integrity**: Selected seat count must exactly match the declared number of seats.
- **Live Seat Reservation**: Pending requests temporarily lock their seats on the seat map, preventing race-condition double selections.
- **Request Tracking**: Categorized list of all travel requests (Pending, Accepted, Rejected, Cancelled).
- **Dynamic Profiles**: Integrated profile and banner image management with file type and size validation.

### 6. 🤖 ARIA AI Chatbot (Gemini Integration)
- **Hyper-Contextual**: Directly integrated with the database to answer questions about *your* specific bookings.
- **Advanced Failover**: Round-robin API key management for 100% uptime.
- **Universal Presence**: Accessible from every page in the application.

### 7. 💎 "SRC Elite" Loyalty Program
- **Holographic membership card**: Centered on the rewards dashboard featuring a premium 3D magnetic hover tilt and cursor-following glare glow.
- **Voucher Redemption Center**: Interchange loyalty points earned from rides into PKR 200, PKR 500, or PKR 1,000 ticket discount vouchers.
- **Minimum Fare Requirements**: Business rules enforce trip subtotal thresholds for voucher usage (Min PKR 500 / PKR 1,200 / PKR 2,500) to keep redemptions realistic and validated at every stage.

### 8. ⚡ Enterprise Performance Optimizations
- **Static Styles Compilation**: Replaced 3.2MB JIT client-side Tailwind Play CDN with statically compiled 45KB `public.css` styles to solve layout shifts and page delays.
- **PDF Document Generators**: Download landscape tabular records, summary analytics charts, and formatted ticket lists as PDFs directly in-browser.
- **Low-End Power Optimization**: Automatically bypass GSAP ScrollTrigger scales and background slides on mobile/low-power engines to keep response speeds at 60fps.

---

## 🛡️ System-Wide Business Logic Summary

| Portal | Validation | Status |
|---|---|---|
| Admin | Last-admin lockout prevention | ✅ |
| Admin | Duplicate bus number / route block | ✅ |
| Admin | Cascade-safe delete (routes, schedules) | ✅ |
| Admin | Schedule minimum duration (30 min) | ✅ |
| Employee | Bus capacity limit on seat assignment | ✅ |
| Employee | Seat double-booking (Pending + Accepted + Completed) | ✅ |
| Employee | Voucher `MinimumFareRequired` re-validated at accept | ✅ |
| Customer | Past travel date rejection | ✅ |
| Customer | Seat count consistency check | ✅ |
| Customer | Voucher minimum fare validated at submit | ✅ |
| Customer | Pending seats locked in live seat map | ✅ |
| Customer | Cancel guard (Pending only) | ✅ |
| Customer | PDF ticket guard (Accepted/Completed only) | ✅ |
| Customer | One-review-per-trip enforcement | ✅ |

---

## 📚 Detailed Module Documentation

For an in-depth look at each specific part of the system, please refer to the dedicated documentation files:

- 👑 **[Admin Portal Documentation](./readme/ADMIN_README.md)**: Fleet management, personnel CRUD, reports, and global controls.
- 🛠️ **[Employee Portal Documentation](./readme/EMPLOYEE_README.md)**: Operational processing, ticket generation, and booking history.
- 👤 **[Customer Portal Documentation](./readme/CUSTOMER_README.md)**: Smart booking, status tracking, notifications, and profile management.
- 🌐 **[Public Experience & Auth Documentation](./readme/PUBLIC_PAGES_README.md)**: GSAP animations, landing page search, and the dual-panel auth system.


*This project is built with ASP.NET Core MVC, Entity Framework Core, and a heavy emphasis on modern Front-end excellence.*
