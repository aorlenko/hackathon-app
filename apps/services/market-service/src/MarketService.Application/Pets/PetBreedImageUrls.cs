using System.Collections.Frozen;

namespace MarketService.Application.Pets;

/// <summary>
/// Static breed name → representative image URL (Wikimedia Commons), aligned with seed breeds and pet_breed_image_urls.csv.
/// </summary>
public static class PetBreedImageUrls
{
    private static readonly FrozenDictionary<string, string> ByBreedName = CreateDictionary();

    public static string? TryGetByBreedName(string breedName) =>
        ByBreedName.TryGetValue(breedName, out var url) ? url : null;

    private static FrozenDictionary<string, string> CreateDictionary()
    {
        var pairs = new (string Breed, string Url)[]
        {
            ("Labrador", "https://upload.wikimedia.org/wikipedia/commons/thumb/3/34/Labrador_on_Quantock_%282175262184%29.jpg/250px-Labrador_on_Quantock_%282175262184%29.jpg"),
            ("Beagle", "https://upload.wikimedia.org/wikipedia/commons/thumb/5/55/Beagle_600.jpg/250px-Beagle_600.jpg"),
            ("Poodle", "https://upload.wikimedia.org/wikipedia/commons/thumb/f/f8/Full_attention_%288067543690%29.jpg/250px-Full_attention_%288067543690%29.jpg"),
            ("Bulldog", "https://upload.wikimedia.org/wikipedia/commons/thumb/b/bf/Bulldog_inglese.jpg/250px-Bulldog_inglese.jpg"),
            ("Pit Bull", "https://upload.wikimedia.org/wikipedia/commons/thumb/7/78/Pit_bull-type_dog_breed_sampler.jpg/250px-Pit_bull-type_dog_breed_sampler.jpg"),
            ("Siamese", "https://upload.wikimedia.org/wikipedia/commons/thumb/1/16/Siamese_cat_Vaillante.JPG/250px-Siamese_cat_Vaillante.JPG"),
            ("Persian", "https://upload.wikimedia.org/wikipedia/commons/thumb/8/81/Persialainen.jpg/250px-Persialainen.jpg"),
            ("Maine Coon", "https://upload.wikimedia.org/wikipedia/commons/thumb/8/87/M%C3%A2le_Black_Silver_Blotched_Tabby.jpeg/250px-M%C3%A2le_Black_Silver_Blotched_Tabby.jpeg"),
            ("Bengal", "https://upload.wikimedia.org/wikipedia/commons/thumb/b/ba/Paintedcats_Red_Star_standing.jpg/250px-Paintedcats_Red_Star_standing.jpg"),
            ("Sphynx", "https://upload.wikimedia.org/wikipedia/commons/thumb/8/89/Sphynx_-_cat._img_031.jpg/250px-Sphynx_-_cat._img_031.jpg"),
            ("Parakeet", "https://upload.wikimedia.org/wikipedia/commons/thumb/1/1a/Melopsittacus_undulatus_-Atlanta_Zoo%2C_Georgia%2C_USA-8a-2c.jpg/250px-Melopsittacus_undulatus_-Atlanta_Zoo%2C_Georgia%2C_USA-8a-2c.jpg"),
            ("Canary", "https://upload.wikimedia.org/wikipedia/commons/thumb/c/ce/GelbA.JPG/330px-GelbA.JPG"),
            ("Cockatiel", "https://upload.wikimedia.org/wikipedia/commons/thumb/8/8a/Cockatiel_3.jpg/250px-Cockatiel_3.jpg"),
            ("Macaw", "https://upload.wikimedia.org/wikipedia/commons/thumb/6/6d/Blue-and-Yellow-Macaw.jpg/250px-Blue-and-Yellow-Macaw.jpg"),
            ("Lovebird", "https://upload.wikimedia.org/wikipedia/commons/thumb/8/88/Agapornis_roseicollis_-eating_grass_seeds-8.jpg/250px-Agapornis_roseicollis_-eating_grass_seeds-8.jpg"),
            ("Goldfish", "https://upload.wikimedia.org/wikipedia/commons/thumb/6/65/Gold_fish1.jpg/250px-Gold_fish1.jpg"),
            ("Betta", "https://upload.wikimedia.org/wikipedia/commons/thumb/1/10/HM_Orange_M_Sarawut.jpg/250px-HM_Orange_M_Sarawut.jpg"),
            ("Guppy", "https://upload.wikimedia.org/wikipedia/commons/thumb/a/a2/Guppy_pho_0048.jpg/250px-Guppy_pho_0048.jpg"),
            ("Angelfish", "https://upload.wikimedia.org/wikipedia/commons/thumb/1/11/Pterophyllum_scalare_Natural_History_Museum_University_of_Pisa.jpg/250px-Pterophyllum_scalare_Natural_History_Museum_University_of_Pisa.jpg"),
            ("Clownfish", "https://upload.wikimedia.org/wikipedia/commons/thumb/a/ad/Amphiprion_ocellaris_%28Clown_anemonefish%29_by_Nick_Hobgood.jpg/250px-Amphiprion_ocellaris_%28Clown_anemonefish%29_by_Nick_Hobgood.jpg"),
        };

        return pairs.ToFrozenDictionary(p => p.Breed, p => p.Url, StringComparer.Ordinal);
    }
}
