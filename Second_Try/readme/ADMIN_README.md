# 👑 Admin Portal Documentation

## 📄 Overview
The Admin Portal is the central nervous system of the SRCTravel platform. It provides high-level administrators with complete control over the fleet, personnel, and financial reporting.

---

## ✨ Key Features

### 1. 👥 Manage Employees
- **Full CRUD**: Create, view, update, and remove employee accounts.
- **Access Control**: Block or unblock employees instantly to manage system access.
- **Search & Filter**: Find employees by name, email, or status.
- **Last-Admin Guard**: The system prevents deactivating the last remaining active Admin account to avoid lockout.
- **Duplicate Email Block**: Creating an employee with an already-registered email is rejected with a clear error message.
- **Password Policy**: New employee passwords must be at least 8 characters.

### 2. 🚌 Fleet Management (Buses)
- **Configuration**: Define bus types (Standard, Luxury, Express, Economy).
- **Tracking**: Assign unique license plates and seating capacities.
- **Capacity Constraints**: Bus capacity is enforced between 10 and 100 seats.
- **Duplicate Bus Guard**: Attempting to register a bus number that already exists returns an error.

### 3. 🗺️ Routes & Pricing
- **Network Mapping**: Create origin and destination pairs with automated trip duration estimation.
- **Same-City Block**: Routes where origin and destination are the same city are rejected.
- **Dynamic Pricing**: Set specific fares for different bus classes on each route.
- **Cascade-Safe Deletion**: Routes with existing schedules, prices, or bookings cannot be hard-deleted; deactivation is offered instead.

### 4. ⏰ Advanced Scheduling
- **Time Management**: Set precise Departure and Arrival times. Requires a minimum arrival gap of at least 30 minutes.
- **Schedule Toggle**: Activate or deactivate specific bus runs without deleting them.
- **Booking-Safe Deletion**: Schedules with associated booking requests cannot be deleted — only deactivated.

### 5. 📊 Reports & Analytics
- **Financial Tracking**: View total revenue and monthly earnings.
- **Booking Trends**: Analyze data through visual stat cards and historical logs.
- **PDF Exporter**: Download high-contrast landscape PDF layouts of booking histories and metrics directly in-browser.

---

## 🛠️ How It Works
- **Sidebar Navigation**: A persistent, glassmorphic sidebar allows quick switching between modules.
- **Responsive Layout**: The dashboard uses a CSS Grid that reconfigures from 4 columns on desktop to 1 column on mobile.
- **Modal-Driven Actions**: Adding or editing data (Schedules, Employees) happens in sleek, non-intrusive modals to keep the user in context.
- **Real-time Feedback**: Integrated with `SweetAlert2` for success/error notifications on every action.

---

## 📱 Mobile Experience
- **Off-canvas Sidebar**: On mobile, the sidebar slides out from the left with a blurred backdrop.
- **Data Tables**: All tables are wrapped in responsive containers with horizontal scrolling to prevent layout breakage on small screens.
