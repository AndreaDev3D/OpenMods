# OpenMods 🚀

**OpenMods** is a state-of-the-art platform designed to revolutionize the way open-source mods are discovered, managed, and distributed. Built with a focus on developer experience and visual excellence, it bridges the gap between GitHub repositories and modding communities.

---

## ✨ Key Features

### 👤 For Modders (Developers)
- **GitHub Integration**: Connect your repositories directly via secure GitHub OAuth (PKCE).
- **Modder Dashboard**: Manage your synced projects, track downloads, and view analytics in a premium, glassmorphic interface.
- **Dynamic Onboarding**: Simple workflow to turn a GitHub repo into a polished OpenMods listing.
- **Auto-Sync**: Keep your mod descriptions and metadata in sync with your source code.

### 🔍 For Users
- **Discovery Hub**: Explore high-quality open-source mods with smart filtering and categories.
- **Deep Documentation**: Rich mod detail pages with README rendering and version history.
- **Themed Experience**: Full support for high-contrast dark mode and premium typography.

---

## 🛠 Tech Stack

- **Frontend/Backend**: [Blazor Server](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) (.NET 10)
- **Authentication**: [Supabase Auth](https://supabase.com/auth) (GitHub OAuth with PKCE)
- **Database**: [Supabase PostgreSQL](https://supabase.com/database) with [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- **Styling**: [Tailwind CSS](https://tailwindcss.com/) with a custom design system
- **State Management**: Scoped session persistence with encrypted cookie backup

---

## 🚀 Getting Started

### Prerequisites
- .NET 10 SDK
- A Supabase project (for Auth and Database)

### Local Development

1. **Clone the repository**:
   ```bash
   git clone https://github.com/AndreaDev3D/OpenMods.git
   cd OpenMods/OpenMods
   ```

2. **Configure Environment Variables**:
   Create a `.env` file in the `OpenMods.Server` directory:
   ```env
   CONNECTION_STRING=your_postgresql_connection_string
   SUPABASE_URL=your_supabase_url
   SUPABASE_ANON_KEY=your_supabase_anon_key
   ```

3. **Run the application**:
   ```bash
   dotnet run --project OpenMods.Server
   ```

---

## 🛡 Security & Architecture

OpenMods uses a modern **PKCE (Proof Key for Code Exchange)** flow for GitHub authentication, ensuring that your developer tokens are never exposed. User sessions are persisted via secure cookies to provide a seamless SPA-like experience across page reloads in a Blazor Server environment.

---

## 📜 License

This project is open-source. See the license file for more details.

---
*Built with ❤️ for the Modding Community.*
