---
name: TongaKids Read System
colors:
  surface: '#fbf9f8'
  surface-dim: '#dcd9d9'
  surface-bright: '#fbf9f8'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f6f3f2'
  surface-container: '#f0eded'
  surface-container-high: '#eae8e7'
  surface-container-highest: '#e4e2e1'
  on-surface: '#1b1c1c'
  on-surface-variant: '#564334'
  inverse-surface: '#303030'
  inverse-on-surface: '#f3f0f0'
  outline: '#8a7362'
  outline-variant: '#ddc1ae'
  surface-tint: '#914c00'
  primary: '#914c00'
  on-primary: '#ffffff'
  primary-container: '#ff8a00'
  on-primary-container: '#613100'
  inverse-primary: '#ffb77f'
  secondary: '#006688'
  on-secondary: '#ffffff'
  secondary-container: '#58cafe'
  on-secondary-container: '#005370'
  tertiary: '#006e1c'
  on-tertiary: '#ffffff'
  tertiary-container: '#5abd5c'
  on-tertiary-container: '#00480f'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#ffdcc4'
  primary-fixed-dim: '#ffb77f'
  on-primary-fixed: '#2f1500'
  on-primary-fixed-variant: '#6f3900'
  secondary-fixed: '#c2e8ff'
  secondary-fixed-dim: '#75d1ff'
  on-secondary-fixed: '#001e2b'
  on-secondary-fixed-variant: '#004d67'
  tertiary-fixed: '#94f990'
  tertiary-fixed-dim: '#78dc77'
  on-tertiary-fixed: '#002204'
  on-tertiary-fixed-variant: '#005313'
  background: '#fbf9f8'
  on-background: '#1b1c1c'
  surface-variant: '#e4e2e1'
typography:
  display-lg:
    fontFamily: Nunito Sans
    fontSize: 48px
    fontWeight: '800'
    lineHeight: 56px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Nunito Sans
    fontSize: 32px
    fontWeight: '700'
    lineHeight: 40px
  headline-lg-mobile:
    fontFamily: Nunito Sans
    fontSize: 28px
    fontWeight: '700'
    lineHeight: 36px
  title-lg:
    fontFamily: Nunito Sans
    fontSize: 22px
    fontWeight: '700'
    lineHeight: 28px
  body-lg:
    fontFamily: Nunito Sans
    fontSize: 18px
    fontWeight: '500'
    lineHeight: 26px
  body-md:
    fontFamily: Nunito Sans
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  label-lg:
    fontFamily: Nunito Sans
    fontSize: 14px
    fontWeight: '700'
    lineHeight: 20px
    letterSpacing: 0.1px
  phonics-focus:
    fontFamily: Nunito Sans
    fontSize: 64px
    fontWeight: '800'
    lineHeight: 80px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  base: 8px
  xs: 4px
  sm: 12px
  md: 16px
  lg: 24px
  xl: 32px
  gutter: 16px
  margin-mobile: 20px
---

## Brand & Style

The design system is crafted to support the educational journey of children learning Chitonga phonics. The brand personality is **nurturing, energetic, and encouraging**. It balances the playfulness of a learning game with the structural integrity of a professional educational tool. 

The aesthetic follows a **Tactile-Modern** approach, building upon Material Design 3 principles. It utilizes "squishy" high-confidence touch targets, soft-edged containers, and a depth model that mimics physical paper and felt layers. This creates a safe, inviting environment that reduces the cognitive load for young learners (ages 5-10) while maintaining a sense of rhythmic fun.

## Colors

The palette is rooted in a **Soft Cream** background to reduce eye strain and provide a warmer, more organic feel than pure white. 

- **Primary (Warm Orange):** Used for main actions, phonics highlights, and "Eureka" moments. It represents energy and growth.
- **Secondary (Sky Blue):** Used for secondary navigation, information bubbles, and passive UI elements. It provides a calming contrast to the primary orange.
- **Accent (Leaf Green):** Reserved for progress, success states, and nature-themed interactive elements.
- **Surface Tints:** Use 5-10% opacity versions of the Primary and Secondary colors for container backgrounds to maintain a vibrant but legible interface.

## Typography

This design system uses **Nunito Sans** for its rounded terminals and open apertures, which are highly legible and friendly for emerging readers. 

- **Weight Usage:** Use ExtraBold (800) for Phonics Focus and Display levels to create a strong visual anchor. Use SemiBold (600) or Bold (700) for interactive labels to ensure high readability.
- **Phonics Focus:** A specialized level for displaying individual letters or syllables. It should be centered with generous whitespace.
- **Accessibility:** Ensure a minimum contrast ratio of 4.5:1 against the Soft Cream background. Avoid using thin weights (under 400) entirely.

## Layout & Spacing

The layout follows a **Fluid Grid** model optimized for Android handsets and tablets. 

- **Margins:** 20px screen margins provide a "safe zone" for small hands gripping the device.
- **Touch Targets:** All interactive elements (buttons, chips, icons) must maintain a minimum hit area of 48x48dp. For the primary phonics cards, increase this to 72x72dp.
- **Vertical Rhythm:** Use a strict 8px baseline grid. Spacing between card groups should be `lg` (24px), while internal content spacing should use `sm` (12px).

## Elevation & Depth

Visual hierarchy is established through **Tonal Layers** and **Soft Ambient Shadows**. 

1. **Level 0 (Background):** Soft Cream.
2. **Level 1 (Cards/Surface):** White with a 1px border of #E0D8C3 (a darker cream) and a very soft, diffused shadow (Blur: 8px, Y: 4px, Opacity: 8% Neutral).
3. **Level 2 (Active/Floating):** Use Primary or Secondary color fills with a "thick" bottom border (3px) in a slightly darker shade of the same hue to create a 3D "button" effect.
4. **Modals:** Use a heavy background blur (15px) on the content behind to maintain focus on the learning task.

## Shapes

The shape language is **Rounded and Organic**. Sharp corners are strictly avoided to maintain the child-friendly safety metaphor. 

- **Standard Elements:** Use `rounded` (0.5rem) for input fields and small UI panels.
- **Primary Buttons/Cards:** Use `rounded-lg` (1rem) to emphasize their interactive nature.
- **Chips & Progress Bars:** Use `rounded-xl` (1.5rem) or full pill-shapes for a smooth, tactile feel.
- **Illustrations:** Frame illustrations within circular or organically "blob" shaped containers to reinforce the playful theme.

## Components

### Buttons
- **Primary:** Warm Orange fill with a 4px "press" depth. Text is White, Bold.
- **Secondary:** Sky Blue ghost buttons with a 2px stroke.
- **Phonics Toggle:** Large cards that "pop" (scale up by 5%) when tapped.

### Progress & Rewards
- **Progress Rings:** Leaf Green tracks with a Soft Cream background. Include a star icon in the center that glows when the task is 100% complete.
- **Star System:** Use a 5-star rating layout for lesson completion. Unearned stars are "Empty/Outline" Sky Blue; earned stars are "Filled" Amber with a slight outer glow.

### Cards
- Standard cards use a White background. Phonics-specific cards can use tinted backgrounds (Light Blue or Light Orange) to categorize different types of sounds (e.g., vowels vs. consonants).

### Bottom Navigation
- Uses the Material Design 3 navigation bar spec but with enlarged icons (32dp). Active states are indicated by a Leaf Green pill-shaped highlight behind the icon.

### Input Fields
- Phonics "Drag and Drop" slots should have a dashed border when empty and a solid Leaf Green border when a correct letter is placed.