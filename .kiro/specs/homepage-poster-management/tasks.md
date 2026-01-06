# Implementation Plan

- [x] 1. Add poster management functions to mockDataStore

  - [x] 1.1 Add STORAGE_KEYS.posters and defaultPosters array

    - Add "mockStore_posters" key to STORAGE_KEYS
    - Create defaultPosters array with 3 slider and 4 promo posters using existing images
    - _Requirements: 7.1, 7.2_

  - [x] 1.2 Implement poster CRUD functions

    - Add getPosters(), getAllPosters(), getPosterById(id)
    - Add createPoster(data) with validation and auto-ID generation
    - Add updatePoster(id, data) preserving ID
    - Add deletePoster(id)
    - _Requirements: 1.1, 1.2, 2.2, 2.4, 3.2_

  - [ ]\* 1.3 Write property test for poster CRUD persistence
    - **Property 1: Poster CRUD Persistence**
    - **Validates: Requirements 1.1, 2.2, 3.2, 7.1, 9.1**
  - [ ]\* 1.4 Write property test for poster validation
    - **Property 2: Poster Validation**
    - **Validates: Requirements 1.2**
  - [ ]\* 1.5 Write property test for poster type validation
    - **Property 3: Poster Type Validation**
    - **Validates: Requirements 1.4**
  - [ ]\* 1.6 Write property test for edit preserves ID
    - **Property 4: Edit Preserves ID**
    - **Validates: Requirements 2.4**
  - [ ]\* 1.7 Write property test for delete removes poster
    - **Property 5: Delete Removes Poster**
    - **Validates: Requirements 3.2**
  - [x] 1.8 Implement getSliderPosters() and getPromoPosters() helper functions

    - Filter by type and isActive
    - Sort by displayOrder ascending, then by ID ascending
    - _Requirements: 4.1, 5.1, 5.3, 8.2, 9.2, 9.3_

  - [ ]\* 1.9 Write property test for active filtering
    - **Property 7: Active Filtering**
    - **Validates: Requirements 4.1, 8.2**
  - [ ]\* 1.10 Write property test for display order sorting
    - **Property 10: Display Order Sorting**
    - **Validates: Requirements 5.3, 9.2, 9.3**
  - [x] 1.11 Add poster subscription support to listeners object

    - Add "posters" to listeners object
    - Ensure notify("posters") is called on all poster modifications
    - _Requirements: 3.4, 7.3_

  - [ ]\* 1.12 Write property test for real-time sync via subscription
    - **Property 6: Real-time Sync via Subscription**
    - **Validates: Requirements 3.4, 7.3**
  - [ ]\* 1.13 Write property test for data loading on init
    - **Property 11: Data Loading on Init**
    - **Validates: Requirements 7.2**

- [x] 2. Checkpoint - Ensure all mockDataStore tests pass

  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Update PosterManagement.jsx to use mockDataStore

  - [x] 3.1 Replace API calls with mockDataStore functions

    - Import mockDataStore
    - Replace axios.get("/api/banners") with mockDataStore.getAllPosters()
    - Replace axios.post with mockDataStore.createPoster()
    - Replace axios.put with mockDataStore.updatePoster()
    - Replace axios.delete with mockDataStore.deletePoster()
    - _Requirements: 1.1, 2.2, 3.2_

  - [x] 3.2 Add dimension guidelines to the form

    - Show "Slider: 1200x400px" and "Promo: 300x200px" recommendations
    - Display specific dimension when type is selected
    - _Requirements: 6.1, 6.2_

  - [x] 3.3 Add real-time subscription for poster updates
    - Subscribe to mockDataStore poster changes on mount
    - Unsubscribe on unmount
    - _Requirements: 7.3_

- [x] 4. Update Home.js with poster slider and promo grid

  - [x] 4.1 Add poster state and data loading

    - Add sliderPosters and promoPosters state
    - Load posters from mockDataStore on mount
    - Subscribe to poster changes for real-time updates
    - _Requirements: 4.1, 5.1, 7.4_

  - [x] 4.2 Implement auto-rotating slider component

    - Add currentSlide state
    - Implement 5-second auto-rotation with setInterval
    - Clear interval on unmount
    - _Requirements: 4.2_

  - [ ]\* 4.3 Write property test for auto-rotation timing

    - **Property 8: Auto-rotation Timing**
    - **Validates: Requirements 4.2**

  - [x] 4.4 Implement slider UI with navigation dots
    - Render slider posters with fade transition
    - Show navigation dots only when multiple posters exist
    - Handle poster click navigation
    - Show default hero banner when no slider posters
    - _Requirements: 4.3, 4.4, 4.5_
  - [ ]\* 4.5 Write property test for navigation dots conditional rendering

    - **Property 9: Navigation Dots Conditional Rendering**
    - **Validates: Requirements 4.3**

  - [x] 4.6 Implement promo grid section
    - Render promo posters in 2x2 (mobile) / 4-column (desktop) grid
    - Handle promo click navigation
    - Hide section when no promo posters exist
    - _Requirements: 5.1, 5.2, 5.4_
  - [ ]\* 4.7 Write property test for promo grid display
    - **Property 12: Promo Grid Display**
    - **Validates: Requirements 5.1**

- [ ] 5. Final Checkpoint - Ensure all tests pass

  - Ensure all tests pass, ask the user if questions arise.
