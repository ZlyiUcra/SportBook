import '@testing-library/jest-dom'

/** jsdom has no ResizeObserver - cmdk (CityCombobox) needs one just to mount its popover content. */
class ResizeObserverStub implements ResizeObserver {
  observe(): void {}
  unobserve(): void {}
  disconnect(): void {}
}

if (!('ResizeObserver' in globalThis)) {
  globalThis.ResizeObserver = ResizeObserverStub
}
