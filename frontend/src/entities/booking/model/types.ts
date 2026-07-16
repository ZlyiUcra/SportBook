export type BookingStatus = 'Pending' | 'Confirmed' | 'Cancelled' | 'Completed'

export type Booking = {
  id: string
  courtId: string
  userId: string
  startTime: string
  endTime: string
  status: BookingStatus
  totalPrice: number
  createdAt: string
}

export type FreeSlot = {
  start: string
  end: string
}

export type Availability = {
  courtId: string
  date: string
  freeSlots: FreeSlot[]
}
