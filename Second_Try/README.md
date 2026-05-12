# SRCTravel — Online Bus Ticket Reservation System

## Current Status
✅ **Phase 2 — Customer Portal: FULLY COMPLETE**
Authentication + Customer Dashboard + Booking Requests + Profile Management + Sign Out + Notifications are all working.

---

## UI / UX Design System
- **Theme Style**: Frosted Aura (Clean Light Theme)
- **Typography**: Inter (Google Fonts)
- **Color Palette** (`wwwroot/css/variables.css`):
  - `var(--bg-primary)`: `#D4DDE2` — Frosted gray page background
  - `var(--bg-secondary)`: `#FFFFFF` — White cards and panels
  - `var(--accent-cyan)`: `#5C7E8F` — Buttons, highlights, sidebar
  - `var(--text-light)`: `#1F2937` — Primary readable text
  - `var(--text-muted)`: `#4B5563` — Muted / secondary text
  - `var(--border-color)`: `#A2A2A2` — Pewter gray borders
  - `var(--success)`: `#3FB950`
  - `var(--danger)`: `#F85149`

---

## Phase 1 — Authentication ✅ COMPLETE

- ✅ Cookie Authentication configured in `Program.cs` (30-day expiry).
- ✅ `BCrypt.Net-Next` for secure password hashing.
- ✅ `AuthController` fully implemented:
  - `GET  /Auth/Login` — Premium sliding panel view.
  - `POST /Auth/Register` — Validates, hashes password, saves Customer, auto-login.
  - `POST /Auth/Login` — Verifies hash, issues role-based auth cookie.
  - `GET  /Auth/Logout` — Destroys cookie, redirects to Login. *(GET + POST both supported)*
- ✅ Real-time Password Strength Meter (5-level, Red → Green).
- ✅ Role-based `[Authorize]` attributes:
  - `CustomerController` → `[Authorize(Roles = "Customer")]`
  - `AdminController` → `[Authorize(Roles = "Admin")]`
  - `EmployeeController` → `[Authorize(Roles = "Employee,Admin")]`
- ✅ Database migration `InitialCreate` — all 8 tables in `SRCTravelDb`.

---

## Phase 2 — Customer Portal ✅ COMPLETE

### Layout & Theme
- ✅ `_CustomerLayout.cshtml` — Fixed sidebar + fixed topbar + responsive hamburger.
- ✅ Profile picture shown in sidebar & topbar avatars dynamically after upload.
- ✅ **Notification bell** — Working dropdown with real DB notifications, unread badge, "Mark all read" button, closes on outside click.
- ✅ Logout link works from sidebar, topbar dropdown, and Profile danger zone.

### C-02 — Dashboard (`/Customer/Dashboard`) — 100% DYNAMIC
- ✅ Welcome banner with first name greeting.
- ✅ 4 live stat cards: Total / Pending / Accepted / Rejected+Cancelled (from DB).
- ✅ **Active Request Status card** — Shows real route, travel date, bus class, seats, status badge.
- ✅ Shows empty state when no active request.
- ✅ **Notifications panel** — Real DB records with time-ago formatting ("Just now", "2h ago", etc.)
- ✅ Empty state when no notifications.
- ✅ Quick Actions grid (Book, My Requests, Profile).

### C-03 — New Booking Request (`/Customer/NewRequest`)
- ✅ 60/40 split-screen — Form + sticky Fare Summary panel.
- ✅ 7 city origin/destination dropdowns with same-city validation.
- ✅ 4 bus type selector cards: Economy, Standard, Luxury, Express.
- ✅ Real-time JavaScript fare calculator.
- ✅ Active-request guard — blocks duplicate pending/accepted requests.
- ✅ Auto-creates DB `Route` if combination doesn't exist.

### C-04 — My Requests (`/Customer/MyRequests`)
- ✅ Full request history from DB (with Route & AssignedBooking).
- ✅ Client-side filter tabs: All / Pending / Accepted / Rejected / Cancelled.
- ✅ Real-time search bar.
- ✅ Cancel Request modal (Pending requests only).
- ✅ "View Ticket" link appears when `AssignedBooking` is populated.

### C-05 — My Profile (`/Customer/Profile`)
- ✅ Hero cover banner with gradient background.
- ✅ **Custom Banner Upload** — Hover cover → "Change Cover" overlay → click → live preview → saves to `wwwroot/uploads/banners/`.
- ✅ **Profile Picture Upload** — Click avatar → live preview → saves to `wwwroot/uploads/profiles/`.
- ✅ Both uploads: old file deleted, new file saved with timestamped name.
- ✅ 3-tab interface:
  - **Personal Info** — Edit Full Name, Email, Phone. Duplicate email check.
  - **Security** — Change Password with BCrypt verification + live strength meter.
  - **Account Stats** — Member Since, stat boxes, account detail card.
- ✅ Tab state preserved across redirects via `TempData["ActiveTab"]`.

---

## Database Migrations Applied

| Migration | Change |
|---|---|
| `InitialCreate` | All 8 core tables |
| `AddBannerUrl` | Added `CoverPictureUrl` column to `Customers` |

---

## Project Structure (Key Files)

```
Controllers/
  AuthController.cs         — Login, Register, Logout (GET+POST)
  CustomerController.cs     — Dashboard, NewRequest, MyRequests, CancelRequest,
                              Profile, UpdateProfile, UpdatePassword,
                              UploadPicture, UploadBanner, MarkNotificationsRead

Models/
  Customer.cs               — FullName, Email, PhoneNumber,
                              ProfilePictureUrl, CoverPictureUrl, CreatedAt
  BookingRequest.cs         — Status (Pending/Accepted/Rejected/Cancelled)
  Notification.cs           — Title, Message, IsRead, CreatedAt
  Route.cs, Bus.cs, Booking.cs, PriceList.cs
  ViewModels/
    AuthPageViewModel.cs, RegisterViewModel.cs, LoginViewModel.cs
    ProfileViewModels.cs    — UpdateProfileViewModel, UpdatePasswordViewModel

Views/
  Shared/_CustomerLayout.cshtml   — Sidebar, topbar, notification bell dropdown
  Customer/
    Dashboard.cshtml        — 100% dynamic stats + active request + notifications
    NewRequest.cshtml       — Booking form + live fare calculator
    MyRequests.cshtml       — Filter tabs, request cards, cancel modal
    Profile.cshtml          — Hero cover, banner/avatar upload, 3 tabs

wwwroot/css/customer/
  customer-dashboard.css    — Layout, sidebar, topbar, cards, toasts, notif dropdown
  new-request.css           — Booking form, bus type grid, fare summary
  my-requests.css           — Filter tabs, request cards, cancel modal
  customer-profile.css      — Hero cover+overlay, avatar, 3-tab interface

wwwroot/uploads/
  profiles/                 — Customer profile pictures
  banners/                  — Customer cover banner photos
```

---

## Known Gaps in Customer Portal (Needs Other Portals First)

| Gap | Root Cause | Resolved By |
|---|---|---|
| "View Ticket" button never shows | `AssignedBooking` only populated when Employee/Admin creates a `Booking` | Employee Dashboard |
| Notifications DB is empty | Nothing writes to `Notifications` table yet | Notification service (Phase 3) |
| Real fare prices | `PriceList` table exists but unused | Admin manages PriceList |
| Auto-expire past requests | No scheduler or check-on-load logic | Phase 3 background service |
| Google OAuth | Button exists, logic not implemented | Phase 3 |

---

## What to Build Next (Recommended Order)

| # | Portal | Feature | Unlocks |
|---|---|---|---|
| 🔴 1 | **Employee Dashboard** | View requests, Accept/Reject with remarks, create Booking record | Customer "View Ticket", full status lifecycle |
| 🔴 2 | **Notification Service** | Write to `Notifications` DB on status changes & registration | Customer notification bell + panel |
| 🟡 3 | **Admin Dashboard** | Manage Routes, Buses, Employees, PriceLists | Real fare calculation |
| 🟡 4 | **Ticket Printing** | Generate PDF ticket when Booking is created | Customer downloads ticket |
| 🟢 5 | **Auto-Expiry** | Auto-cancel pending requests past travel date | Accurate dashboard stats |
| 🟢 6 | **Google OAuth** | Google login button logic | Social login |

---

*This README is continuously updated as the project progresses.*
