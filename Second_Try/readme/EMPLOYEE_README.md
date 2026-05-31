# 🛠️ Employee Portal Documentation

## 📄 Overview
The Employee Portal is an operational hub designed for efficiency. Employees use this interface to manage daily booking traffic, verify passengers, and process travel requests in real-time.

---

## ✨ Key Features

### 1. 📈 Operational Dashboard
- **Quick Stats**: At-a-glance view of Today's Bookings and Pending Requests.
- **Task Focus**: Prioritizes urgent customer requests that require immediate attention.

### 2. ⚡ Request Processing
- **Review System**: Detailed view of customer booking details (Route, Date, Bus Type).
- **Decision Engine**: Single-click "Accept" or "Reject" actions.
- **Bus Capacity Enforcement**: The system validates that the number of seats being assigned does not exceed the selected bus's total seating capacity before confirming any booking.
- **Seat Double-Booking Prevention**: Before accepting a request, the system checks all other requests on the **same schedule and travel date** that are in `Pending`, `Accepted`, or `Completed` status. If any requested seat is already taken or temporarily reserved, the acceptance is blocked with a clear error identifying the conflicting seat.
- **Seat Sync & Release**: If the employee alters seat numbers from the user's initial selection, original requested seats are automatically released so they don't block the seat map for other passengers.
- **Auto-Calculated Fares**: System dynamically calculates totals (assigned seats × base route fare, minus pre-applied customer voucher discounts). Employees cannot alter voucher selections manually.
- **Voucher Minimum Fare Re-validation**: Even if a customer attached a voucher at request time, the system re-validates the `MinimumFareRequired` threshold at the moment of acceptance to prevent discounts on sub-threshold fares.

### 3. 🎫 Digital Ticketing
- **Ticket Generation**: Instant HTML ticket creation for every accepted booking.
- **Print Ready**: Optimized view for printing physical boarding passes for passengers.

### 4. 📜 Booking History
- **Audit Trail**: A complete, searchable log of all past bookings processed by the system.
- **Status Tracking**: Filter by Completed, Accepted, or Rejected statuses.
- **Landscape PDF Exports**: Download fully structured layout grids of booking logs.

---

## 🛡️ Business Logic & Validations

| Check | Details |
|---|---|
| Seat count vs. bus capacity | Rejected if `assignedSeats.Count > bus.Capacity` |
| Seat double-booking | Checked against `Pending`, `Accepted`, and `Completed` requests on same schedule + date |
| Voucher minimum fare | `baseFare × seats ≥ voucher.MinimumFareRequired` enforced at accept time |
| Bus selection required | Empty or zero `busId` is rejected immediately |
| Fare required | Zero fare is only accepted if a valid voucher covers the full amount |

---

## 🛠️ How It Works
- **Processing Flow**: When a customer submits a request, it appears in the Employee's "Process Request" queue. The employee verifies availability and clicks accept.
- **Notification Integration**: The system pushes real-time updates to the customer's dashboard once the employee makes a decision.
- **User Interface**: Built with a clean, high-contrast dark theme to reduce eye strain during long shifts.

---

## 📱 Mobile Experience
- **Responsive Grids**: Stat cards stack vertically on smartphones.
- **Sidebar Navigation**: Uses the same advanced mobile-toggle system as the Admin portal, ensuring full functionality on touch devices.
- **Table Accessibility**: Booking logs feature horizontal scroll wrappers for easy review on the go.
