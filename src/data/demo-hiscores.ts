export interface HiscoreEntry {
  playerName: string;
  score: number;
}

// Preview fixtures only. These entries are not player submissions.
export const demoHiscores: readonly HiscoreEntry[] = [
  { playerName: 'MoonlightMandy', score: 24850 },
  { playerName: 'BillyTheBrave', score: 22100 },
  { playerName: 'CosmicCocoa', score: 19750 },
  { playerName: 'MidnightMischief', score: 18400 },
  { playerName: 'LittleGreenGuest', score: 16950 },
  { playerName: 'GingerbreadGhost', score: 15300 },
  { playerName: 'TinselTrouble', score: 14200 },
  { playerName: 'StarlitSocks', score: 12850 },
  { playerName: 'SnowballBandit', score: 11400 },
  { playerName: 'OneMoreChristmas', score: 9750 },
];
