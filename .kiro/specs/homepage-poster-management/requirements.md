# Requirements Document

## Introduction

Bu özellik, e-ticaret sitesinin ana sayfasındaki poster/banner ve promosyon görsellerinin admin paneli üzerinden yönetilmesini sağlar. Ana sayfada 3 adet slider poster ve 4 adet promosyon görseli bulunacak, bunlar otomatik olarak değişecek ve admin tarafından eklenip, düzenlenip, silinebilecektir. Tüm veriler localStorage'da saklanacak ve ana sayfa bu verileri otomatik olarak algılayıp gösterecektir.

## Glossary

- **Poster/Banner**: Ana sayfada slider olarak gösterilen büyük görseller (1200x400 piksel önerilen)
- **Promosyon Görseli**: Ana sayfada grid şeklinde gösterilen küçük kampanya görselleri (300x200 piksel önerilen)
- **Slider**: Otomatik olarak değişen poster carousel bileşeni
- **localStorage**: Tarayıcıda veri saklamak için kullanılan web storage API'si
- **Admin Panel**: Yönetici kullanıcıların içerik yönetimi yaptığı arayüz
- **mockDataStore**: Uygulama genelinde mock verileri yöneten servis

## Requirements

### Requirement 1

**User Story:** As an admin, I want to add new posters/banners to the homepage, so that I can promote campaigns and products effectively.

#### Acceptance Criteria

1. WHEN an admin submits a new poster form with title, image URL, link URL, display order, and type THEN the PosterManagement component SHALL save the poster to localStorage via mockDataStore
2. WHEN a poster is saved THEN the system SHALL validate that image URL is not empty and title is provided
3. WHEN a poster is successfully added THEN the system SHALL display a success notification and clear the form
4. WHEN adding a poster THEN the system SHALL allow specifying poster type as either "slider" (1200x400px) or "promo" (300x200px)
5. WHEN the form displays THEN the system SHALL show recommended pixel dimensions for each poster type

### Requirement 2

**User Story:** As an admin, I want to edit existing posters, so that I can update campaign information without deleting and recreating.

#### Acceptance Criteria

1. WHEN an admin clicks edit on a poster THEN the PosterManagement component SHALL populate the form with existing poster data
2. WHEN an admin submits the edit form THEN the system SHALL update the poster in localStorage via mockDataStore
3. WHEN a poster is successfully updated THEN the system SHALL display a success notification and reset the form to add mode
4. WHEN editing a poster THEN the system SHALL preserve the original poster ID

### Requirement 3

**User Story:** As an admin, I want to delete posters, so that I can remove outdated or incorrect content.

#### Acceptance Criteria

1. WHEN an admin clicks delete on a poster THEN the system SHALL display a confirmation dialog
2. WHEN the admin confirms deletion THEN the PosterManagement component SHALL remove the poster from localStorage via mockDataStore
3. WHEN a poster is successfully deleted THEN the system SHALL display a success notification and refresh the poster list
4. WHEN a poster is deleted THEN the HomePage component SHALL automatically update to reflect the change

### Requirement 4

**User Story:** As a visitor, I want to see an auto-rotating poster slider on the homepage, so that I can discover current promotions without manual interaction.

#### Acceptance Criteria

1. WHEN the homepage loads THEN the HomePage component SHALL display active slider posters from localStorage
2. WHILE the slider is visible THEN the system SHALL automatically rotate to the next poster every 5 seconds
3. WHEN there are multiple active slider posters THEN the system SHALL display navigation dots for manual selection
4. WHEN a user clicks on a poster THEN the system SHALL navigate to the poster's link URL
5. WHEN no slider posters exist THEN the system SHALL display a default hero banner

### Requirement 5

**User Story:** As a visitor, I want to see promotional images on the homepage, so that I can quickly see current deals and offers.

#### Acceptance Criteria

1. WHEN the homepage loads THEN the HomePage component SHALL display active promo images in a 2x2 or 4-column grid
2. WHEN a promo image is clicked THEN the system SHALL navigate to the promo's link URL
3. WHEN promo images are displayed THEN the system SHALL show them in the order specified by displayOrder field
4. WHEN no promo images exist THEN the system SHALL hide the promo section entirely

### Requirement 6

**User Story:** As an admin, I want to see poster dimension guidelines, so that I can upload correctly sized images.

#### Acceptance Criteria

1. WHEN the PosterManagement form is displayed THEN the system SHALL show recommended dimensions: Slider 1200x400px, Promo 300x200px
2. WHEN an admin selects poster type THEN the system SHALL display the specific dimension requirement for that type
3. WHEN displaying poster preview THEN the system SHALL show the image with correct aspect ratio

### Requirement 7

**User Story:** As a system, I want poster data to persist in localStorage and sync with mockDataStore, so that data survives page refreshes and integrates with the existing architecture.

#### Acceptance Criteria

1. WHEN posters are modified THEN the mockDataStore SHALL save data to localStorage under "posters" key
2. WHEN the application loads THEN the mockDataStore SHALL load poster data from localStorage
3. WHEN poster data changes THEN the mockDataStore SHALL notify all subscribed components via the subscription system
4. WHEN the HomePage component mounts THEN the component SHALL subscribe to poster changes in mockDataStore

### Requirement 8

**User Story:** As an admin, I want to toggle poster active status, so that I can temporarily hide posters without deleting them.

#### Acceptance Criteria

1. WHEN an admin toggles the active checkbox THEN the system SHALL update the poster's isActive status in localStorage
2. WHEN a poster is inactive THEN the HomePage component SHALL exclude it from display
3. WHEN viewing the poster list THEN the PosterManagement component SHALL visually distinguish active and inactive posters

### Requirement 9

**User Story:** As an admin, I want to reorder posters, so that I can control which promotions appear first.

#### Acceptance Criteria

1. WHEN an admin sets a displayOrder value THEN the system SHALL save this order to localStorage
2. WHEN displaying posters THEN the HomePage component SHALL sort posters by displayOrder in ascending order
3. WHEN multiple posters have the same displayOrder THEN the system SHALL maintain consistent ordering by ID
