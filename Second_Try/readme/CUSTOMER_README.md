# 👤 Customer Portal Documentation

## 📄 Overview
The Customer Portal is a premium traveler's dashboard. It provides a seamless interface for users to book trips, manage their travel history, and customize their personal profile.

---

## ✨ Key Features

### 1. 🏠 Traveler Dashboard
- **Welcome Banner**: Personalized greetings and quick stats (Total Bookings, Notifications).
- **Recent Activity**: Quick view of the latest travel updates.

### 2. 📅 Smart Booking System
- **Split-screen Design**: A modern 60/40 layout for searching and selecting buses.
- **Auto-Fill Logic**: If a user searches for a bus on the public homepage, their selection is automatically carried over to the booking form.
- **Availability Filter**: Search by Origin, Destination, and Travel Date.
- **Past Date Rejection**: Submitting a booking request for a date that has already passed is blocked server-side with a clear error — past travel dates are never accepted.
- **Seat Count Consistency Check**: The number of seats listed in `SelectedSeatNumbers` must exactly match the declared `NumberOfSeats`. A mismatch is rejected to prevent seat map desync.
- **Voucher Minimum Fare Validation**: If a discount voucher is attached, the system calculates the trip subtotal and verifies it meets the voucher's `MinimumFareRequired` before the request is submitted.

### 3. 🗺️ Live Seat Map
- **Visual Seat Grid**: An interactive 5-column seat grid dynamically renders available and occupied seats.
- **Real-Time Occupied Seats API**: The `GetBookedSeats` endpoint checks all `Pending`, `Accepted`, and `Completed` requests for the selected schedule and date — meaning even pending (temporarily reserved) seats are shown as unavailable to prevent race conditions.

### 4. 📋 My Travel Requests
- **Status Tracking**: Live tracking of requests through states: `Pending`, `Accepted`, `Rejected`, and `Cancelled`.
- **Filtering**: Quick filters to sort through your travel history.
- **Cancellation**: Only `Pending` requests can be cancelled — accepted bookings cannot be retracted.
- **Trip Reviews**: Submit star ratings and comments for `Completed` trips (one review per trip enforced).
- **PDF Ticket Export**: Generates a formatted PDF boarding pass for `Accepted` and `Completed` requests only. Attempting to download a ticket for any other status is blocked.

### 5. 💎 "SRC Elite" Loyalty Rewards
- **Membership**: Opt-in to receive a welcome bonus of 500 points and access a high-performance 3D magnetic hover card.
- **Points Accumulation**: Earn 5% of fare as points on regular rides, and 10% on Elite rides.
- **Redemption**: Convert points into ticket discount vouchers:

| Voucher | Points Cost | Min. Trip Fare Required |
|---|---|---|
| PKR 200 discount | 500 pts | PKR 500 |
| PKR 500 discount | 1,000 pts | PKR 1,200 |
| PKR 1,000 discount | 1,800 pts | PKR 2,500 |

### 6. 🖼️ Profile & Personalization
- **Dynamic Avatars**: Upload and update profile pictures (JPG/PNG/WebP, max 3 MB).
- **Banner Customization**: Set a custom cover photo for your profile (max 5 MB).
- **Security**: Password changes require the current password and enforce complexity (8+ chars, uppercase, lowercase, digit, special character).
- **Email Uniqueness**: Changing your email is validated for uniqueness across all accounts.
- **Phone Format**: Phone number updates are validated for numeric-only format (with optional `+` prefix).

---

## 🛡️ Business Logic & Validations

| Check | Details |
|---|---|
| Past travel date | Rejected if `TravelDate < DateTime.Today` |
| Seat count mismatch | Rejected if selected seat count ≠ `NumberOfSeats` |
| Voucher minimum fare | Rejected if `baseFare × seats < voucher.MinimumFareRequired` |
| Ticket download guard | Only `Accepted` / `Completed` requests may generate a PDF ticket |
| Cancel guard | Only `Pending` requests can be cancelled |
| Review guard | One review per completed trip, duplicate submission blocked |

---

## 🛠️ How It Works
- **Booking Lifecycle**:
    1. Customer searches and submits a **Request**.
    2. Request status is **Pending** (seat is temporarily reserved in the seat map).
    3. Employee accepts → Status becomes **Accepted** (Ticket Generated).
- **Notifications**: A dynamic bell icon in the topbar alerts customers whenever their request status changes.

---

## 📱 Mobile Experience
- **Fluid Layout**: The dashboard adapts perfectly to all screen sizes using Tailwind CSS.
- **Card-Based UI**: Lists like "My Requests" transform into mobile-optimized cards rather than wide tables.
- **Interactive Sidebar**: Easy access to "Book Trip" and "Profile" via a slide-out mobile menu.
