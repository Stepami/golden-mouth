import eveningSmallCover from '../assets/evening-640-v4.webp'
import housesSmallCover from '../assets/houses-640-v4.webp'
import itSmallCover from '../assets/it-640-v4.webp'
import eveningCover from '../assets/evening-960-v4.webp'
import housesCover from '../assets/houses-960-v4.webp'
import itCover from '../assets/it-960-v4.webp'
export const worldArt: Record<string, { cover: string; smallCover: string; pitch: string; genre: string; smallCoverWidth?: number }> = {
  'aec9494b-4118-4525-8458-e85d759c8dd9': { cover: '/landing/hogwarts-960.jpg', smallCover: '/landing/hogwarts-480.jpg', smallCoverWidth: 480, genre: 'Магия · Интриги', pitch: 'Хогвартс обанкротился. Вернуть школу или превратить замок в отель?' },
  'b3a1a522-0263-4796-a197-57a8731fc291': { cover: eveningCover, smallCover: eveningSmallCover, genre: 'Тайны · Романтика', pitch: 'Исчезнувшие улицы, чужие воспоминания и знакомства после заката.' },
  '8c02a7fb-3b55-4eb0-b957-1b4869ed9834': { cover: housesCover, smallCover: housesSmallCover, genre: 'Интриги · Приключения', pitch: 'Великие дома, драконы и союзы, от которых зависит северное царство.' },
  '5720946a-ea7c-4a10-9c50-51ce3deefc53': { cover: itCover, smallCover: itSmallCover, genre: 'Стартапы · Ирония', pitch: 'Запускай проекты, спорь о технологиях и попадай в стартап-драмы.' },
}
