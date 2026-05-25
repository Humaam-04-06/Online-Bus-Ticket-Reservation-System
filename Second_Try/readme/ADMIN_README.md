# 👑 Admin Portal Documentation

## 📄 Overview
The Admin Portal is the central nervous system of the SRCTravel platform. It provides high-level administrators with complete control over the fleet, personnel, and financial reporting.

---

## ✨ Key Features

### 1. 👥 Manage Employees
- **Full CRUD**: Create, view, update, and remove employee accounts.
- **Access Control**: Block or unblock employees instantly to manage system access.
- **Search & Filter**: Find employees by name, email, or status.

### 2. 🚌 Fleet Management (Buses)
- **Configuration**: Define bus types (Standard, Luxury, Express, Economy).
- **Tracking**: Assign unique license plates and seating capacities.

### 3. 🗺️ Routes & Pricing
- **Network Mapping**: Create origin and destination pairs.
- **Dynamic Pricing**: Set specific fares for different bus classes on each route.
- **Auto-Duration Calculation**: Instantly computes travel duration using coordinates of Pakistan's major cities and the Haversine formula upon route creation.

### 4. ⏰ Advanced Scheduling
- **Time Management**: Set precise Departure and Arrival times.
- **Spacing Validation**: Enforces that arrival times are at least 30 minutes after departure times to prevent scheduling overlaps (client-side and server-side checks).
- **Schedule Toggle**: Activate or deactivate specific bus runs without deleting them.

### 5. 📊 Reports & Analytics
- **Financial Tracking**: View total revenue and monthly earnings.
- **Booking Trends**: Analyze data through visual stat cards and historical logs.
- **Landscape PDF Reports**: Generate and download professional, formatted PDF logs for schedules, employee lists, and global revenue analytics with a single click.

---

## 🛠️ How It Works
- **Sidebar Navigation**: A persistent, glassmorphic sidebar allows quick switching between modules.
- **Responsive Layout**: The dashboard uses a CSS Grid that reconfigures from 4 columns on desktop to 1 column on mobile.
- **Modal-Driven Actions**: Adding or editing data (Schedules, Employees) happens in sleek, non-intrusive modals to keep the user context.
- **Real-time Feedback**: Integrated with `SweetAlert2` for success/error notifications on every action.

---

## 📱 Mobile Experience
- **Off-canvas Sidebar**: On mobile, the sidebar slides out from the left with a blurred backdrop.
- **Data Tables**: All tables are wrapped in responsive containers with horizontal scrolling to prevent layout breakage on small screens.
