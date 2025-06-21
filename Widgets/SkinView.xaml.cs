using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows;
using HelixToolkit.Wpf.SharpDX;
using MineLauncher.BlockBench;
using SharpDX;
using SharpDX.Direct3D11;
using Mat = HelixToolkit.Wpf.SharpDX.Material;
using Rectangle = System.Drawing.Rectangle;

namespace MineLauncher.Widgets;

public partial class SkinView : Viewport3DX
{
    public static readonly DependencyProperty DisplayElytraProperty =
        DependencyProperty.Register(nameof(DisplayElytra), typeof(bool), typeof(SkinView), 
            new PropertyMetadata((o, args) =>
            {
                var skinView = (SkinView)o;
                var displayElytra = (bool)args.NewValue;
                
                skinView.RefreshCapeElytraModelInternal(displayElytra);
            }));
    
    public static readonly DependencyProperty PlayerSlimProperty =
        DependencyProperty.Register(nameof(PlayerSlim), typeof(bool), typeof(SkinView), 
            new PropertyMetadata((o, args) =>
            {
                var skinView = (SkinView)o;
                var slim = (bool)args.NewValue;
                
                skinView.RefreshPlayerModelInternal(slim);
            }));
    
    public static readonly DependencyProperty PlayerSkinProperty =
        DependencyProperty.Register(nameof(PlayerSkin), typeof(Image), typeof(SkinView), 
            new PropertyMetadata((o, args) =>
            {
                var skinView = (SkinView)o;
                var playerSkin = (Image)args.NewValue;
                
                skinView._playerWideMaterial.DiffuseMap = playerSkin is not null
                    ? skinView.CreateTexture(playerSkin)
                    : skinView._wideProject.Textures[0].Source;
                
                skinView._playerSlimMaterial.DiffuseMap = playerSkin is not null
                    ? skinView.CreateTexture(playerSkin)
                    : skinView._slimProject.Textures[0].Source;
            }));
    
    public static readonly DependencyProperty CapeSkinProperty =
        DependencyProperty.Register(nameof(CapeSkin), typeof(Image), typeof(SkinView), 
            new PropertyMetadata((o, args) =>
            {
                var skinView = (SkinView)o;
                var capeSkin = (Image)args.NewValue;
                
                skinView._capeMaterial.DiffuseMap = capeSkin is not null
                    ? skinView.CreateTexture(capeSkin)
                    : skinView._capeProject.Textures[0].Source;
                skinView.RefreshCapeElytraModelInternal(skinView.DisplayElytra);
            }));

    public bool DisplayElytra
    {
        get => (bool)GetValue(DisplayElytraProperty);
        set => SetValue(DisplayElytraProperty, value);
    }
    
    public bool PlayerSlim
    {
        get => (bool)GetValue(PlayerSlimProperty);
        set => SetValue(PlayerSlimProperty, value);
    }
    
    public Image PlayerSkin
    {
        get => (Image)GetValue(PlayerSkinProperty);
        set => SetValue(PlayerSkinProperty, value);
    }
    
    public Image CapeSkin
    {
        get => (Image)GetValue(CapeSkinProperty);
        set => SetValue(CapeSkinProperty, value);
    }
    
    private record struct MaterialSignature
    {
        public int TextureIndex;
    }
    
    private readonly Dictionary<Image, TextureModel> _imageTextures = new();
    
    private Project _wideProject;
    private Project _slimProject;
    private GroupModel3D _wideModel;
    private GroupModel3D _slimModel;
    private DiffuseMaterial _playerWideMaterial;
    private DiffuseMaterial _playerSlimMaterial;
    
    private Project _capeProject;
    private Project _elytraProject;
    private GroupModel3D _capeModel;
    private GroupModel3D _elytraModel;
    private DiffuseMaterial _capeMaterial;
    
    public SkinView()
    {
        Loaded += (s, e) =>
        {
            InitializeComponent();
            InitializePlayerModel();
        };
    }

    private void InitializePlayerModel()
    {
        var account = App.Instance.Account;
        var assembly = Assembly.GetExecutingAssembly();
        
        // BlockBench load
        _wideProject = Project.Load(assembly.GetManifestResourceStream("MineLauncher.Player.bbmodel"));
        _slimProject = Project.Load(assembly.GetManifestResourceStream("MineLauncher.PlayerSlim.bbmodel"));
        _capeProject = Project.Load(assembly.GetManifestResourceStream("MineLauncher.Cape.bbmodel"));
        _elytraProject = Project.Load(assembly.GetManifestResourceStream("MineLauncher.Elytra.bbmodel"));
        
        // Materials
        _playerWideMaterial = CreateMaterial();
        _playerWideMaterial.DiffuseMap = PlayerSkin is not null
            ? CreateTexture(PlayerSkin)
            : _wideProject.Textures[0].Source;
        
        _playerSlimMaterial = CreateMaterial();
        _playerSlimMaterial.DiffuseMap = PlayerSkin is not null
            ? CreateTexture(PlayerSkin)
            : _slimProject.Textures[0].Source;

        _capeMaterial = CreateMaterial();
        _capeMaterial.DiffuseMap = CapeSkin is not null
            ? CreateTexture(CapeSkin)
            : _capeProject.Textures[0].Source;
        
        // Models
        _wideModel = BuildBB(_wideProject, _ => _playerWideMaterial);
        _slimModel = BuildBB(_slimProject, _ => _playerSlimMaterial);
        _capeModel = BuildBB(_capeProject, _ => _capeMaterial);
        _elytraModel = BuildBB(_elytraProject, _ => _capeMaterial);
        
        // Refresh
        RefreshPlayerModelInternal(PlayerSlim);
        RefreshCapeElytraModelInternal(DisplayElytra);
    }

    private void RefreshPlayerModelInternal(bool slim)
    {
        Items.Remove(_slimModel);
        Items.Remove(_wideModel);

        var usedModel = slim
            ? _slimModel
            : _wideModel;
                
        if (usedModel is not null)
            Items.Add(usedModel);
    }

    private void RefreshCapeElytraModelInternal(bool elytra)
    {
        Items.Remove(_elytraModel);
        Items.Remove(_capeModel);

        var usedModel = CapeSkin is null
            ? null
            : elytra
                ? _elytraModel
                : _capeModel;
                
        if (usedModel is not null)
            Items.Add(usedModel);
    }

    private TextureModel CreateTexture(Image image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        
        if (_imageTextures.TryGetValue(image, out var existing))
            return existing;

        using (var bitmap = new Bitmap(image))
        {
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                var length = bitmap.Width * bitmap.Height;
                var colors = new Color4[length];
                var ptr = bitmapData.Scan0;
            
                unsafe
                {
                    var pixels = (int*)ptr.ToPointer();
                    for (int i = 0; i < length; i++)
                    {
                        int pixel = pixels[i];
                        colors[i] = new Color4(
                            ((pixel >> 16) & 0xFF) / 255f,  // R
                            ((pixel >> 8) & 0xFF) / 255f,   // G
                            (pixel & 0xFF) / 255f,          // B
                            ((pixel >> 24) & 0xFF) / 255f); // A
                    }
                }

                var newTex = new TextureModel(colors, bitmap.Width, bitmap.Height);
                _imageTextures[image] = newTex;
                return newTex;
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }
    }

    private DiffuseMaterial CreateMaterial()
    {
        var mat = new DiffuseMaterial();
        mat.DiffuseMapSampler = mat.DiffuseMapSampler with { Filter = Filter.MinMagMipPoint };

        return mat;
    }

    private GroupModel3D BuildBB(Project project, Func<MaterialSignature, Mat> materialProvider = null)
    {
        Func<Group, GroupModel3D> buildGroup;
        Func<Element, Element3D> buildModel;
        Func<IEnumerable<IProjectNode>, IEnumerable<Element3D>> buildChildren = null;
        
        buildGroup = group => new GroupModel3D
        {
            Name = group.Name.ToValidName(),
            ItemsSource = buildChildren(group.GetChildren(project)).ToList(),
            Transform = group.Rotation.IsZero
                ? null
                : Utils.MakeCorrectRotationMatrix(group.Rotation, new Vector3(
                    -group.Origin.X,
                     group.Origin.Y,
                    -group.Origin.Z
                ))
        };

        buildModel = element =>
        {
            var entries = CreateBBElement(project, element, materialProvider);
            bool singleModel = entries.Count == 1;
            
            var meshes = entries.Select((entry, i) => new MeshGeometryModel3D
            {
                Name = singleModel
                    ? element.Name.ToValidName()
                    : $"{element.Name.ToValidName()}_{i}",
                Geometry = entry.Value,
                Material = entry.Key,
                IsTransparent = true,
                CullMode = CullMode.Back,
                Transform = element.Rotation.IsZero
                    ? null
                    : Utils.MakeCorrectRotationMatrix(element.Rotation, new Vector3(
                        -element.Origin.X,
                         element.Origin.Y,
                        -element.Origin.Z
                    ))
            });
            
            if (singleModel)
                return meshes.First();
            else
                return new GroupModel3D
                {
                    Name = element.Name.ToValidName(),
                    ItemsSource = meshes.Cast<Element3D>().ToList(),
                };
        };

        buildChildren = nodes => nodes.Select(node => node switch
        {
            Group group => buildGroup(group),
            Element element => buildModel(element),
            _ => throw new ArgumentException("Unknown node type")
        });
        
        return new GroupModel3D
        {
            ItemsSource = buildChildren(project.Outliner).ToList()
        };
    }

    private Dictionary<Mat, MeshGeometry3D> CreateBBElement(Project project, Element element, Func<MaterialSignature, Mat> materialProvider) => element switch
    {
        CubeElement cube => CreateBBCube(project, cube, materialProvider),
        _ => throw new ArgumentException("Unknown element type")
    };

    private Dictionary<Mat, MeshGeometry3D> CreateBBCube(Project project, CubeElement element, Func<MaterialSignature, Mat> materialProvider)
    {
        Dictionary<Mat, MeshGeometry3D> result = new();
        
        var v = (bool x, bool y, bool z) => new Vector3(
            x ? -element.ActualTo.X : -element.ActualFrom.X,
            y ?  element.ActualTo.Y :  element.ActualFrom.Y,
            z ? -element.ActualTo.Z : -element.ActualFrom.Z
        );
        
        var p = (MeshGeometry3D mesh, Vector3 v) =>
        {
            mesh.Positions ??= new();
            mesh.Positions.Add(v);
            return mesh.Positions.Count - 1;
        };
        
        var tc = (MeshGeometry3D mesh, CubeFace face, bool x, bool y) =>
        {
            var texture = project.Textures[face.Texture];
            mesh.TextureCoordinates ??= new();
            mesh.TextureCoordinates.Add(new Vector2(
                (x ? (float)face.UV.X : (float)face.UV.Width) / texture.Width,
                (y ? (float)face.UV.Y : (float)face.UV.Height) / texture.Height
            ));
        };

        var v0 = v(false, false, false);
        var v1 = v(false, false, true);
        var v2 = v(false, true,  false);
        var v3 = v(false, true,  true);
        var v4 = v(true,  false, false);
        var v5 = v(true,  false, true);
        var v6 = v(true,  true,  false);
        var v7 = v(true,  true,  true);

        var addFace = (CubeFace face, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3) =>
        {
            MaterialSignature sig = new MaterialSignature { TextureIndex = face.Texture};
            if (materialProvider?.Invoke(sig) is { } material)
            {
                MeshGeometry3D mesh;
                if (result.TryGetValue(material, out var existing))
                    mesh = existing;
                else
                {
                    mesh = new();
                    result.Add(material, mesh);
                }
            
                var p0 = p(mesh, v0); tc(mesh, face, true, true);
                var p1 = p(mesh, v1); tc(mesh, face, true, false);
                var p2 = p(mesh, v2); tc(mesh, face, false, false);
                var p3 = p(mesh, v3); tc(mesh, face, false, true);
            
                mesh.TriangleIndices ??= new();
                mesh.TriangleIndices.Add(p0);
                mesh.TriangleIndices.Add(p1);
                mesh.TriangleIndices.Add(p2);
                mesh.TriangleIndices.Add(p0);
                mesh.TriangleIndices.Add(p2);
                mesh.TriangleIndices.Add(p3);   
            }
        };

        if (element.Faces.Front is not null)
            addFace(element.Faces.Front, v6, v4, v0, v2);
        if (element.Faces.Back is not null)
            addFace(element.Faces.Back, v3, v1, v5, v7);
        if (element.Faces.Right is not null)
            addFace(element.Faces.Right, v2, v0, v1, v3);
        if (element.Faces.Left is not null)
            addFace(element.Faces.Left, v7, v5, v4, v6);
        if (element.Faces.Up is not null)
            addFace(element.Faces.Up, v2, v3, v7, v6);
        if (element.Faces.Down is not null)
            addFace(element.Faces.Down, v1, v0, v4, v5);
        
        return result;
    }

    /**
     * Create an optimized box, centered at zero coordinate, provided width, height and depth and uv
     */
    private MeshGeometry3D CreateBox(Vector2 uvOffset, Vector2 textureSize, int w, int h, int d)
    {
        MeshGeometry3D mesh = new();
        
        // Point
        var p = (Vector3 pt) =>
        {
            mesh.Positions ??= new();
            mesh.Positions.Add(pt);
            return mesh.Positions.Count - 1;
        };
        
        // Texture Coordinate
        var tc = (int x, int y) =>
        {
            mesh.TextureCoordinates ??= new();
            mesh.TextureCoordinates.Add(new Vector2(
                (uvOffset.X + x) / textureSize.X,
                (uvOffset.Y + y) / textureSize.Y
            ));
        };
        
        var c = (byte r, byte g, byte b) =>
        {
            mesh.Colors ??= new();
            mesh.Colors.Add(new Color4(r / 255f, g / 255f, b / 255f, 1));
        };

        var rp0 = new Vector3(-w/2f/10, -h/2f/10, -d/2f/10);
        var rp1 = new Vector3(-w/2f/10, -h/2f/10,  d/2f/10);
        var rp2 = new Vector3(-w/2f/10,  h/2f/10, -d/2f/10);
        var rp3 = new Vector3(-w/2f/10,  h/2f/10,  d/2f/10);
        var rp4 = new Vector3( w/2f/10, -h/2f/10, -d/2f/10);
        var rp5 = new Vector3( w/2f/10, -h/2f/10,  d/2f/10);
        var rp6 = new Vector3( w/2f/10,  h/2f/10, -d/2f/10);
        var rp7 = new Vector3( w/2f/10,  h/2f/10,  d/2f/10);
        
        var p0  = p(rp0); tc(d+w+d,   d+h);
        var p1  = p(rp1); tc(d+w,     d+h);
        var p2  = p(rp2); tc(d+w+d,   d);
        var p3  = p(rp3); tc(d+w,     d);
        var p4  = p(rp4); tc(0,       d+h);
        var p5  = p(rp5); tc(d,       d+h);
        var p6  = p(rp6); tc(0,       d);
        var p7  = p(rp7); tc(d,       d);
        var p8  = p(rp0); tc(d+w+d,   0);
        var p9  = p(rp1); tc(d+w+d,   d);
        var p10 = p(rp4); tc(d+w,     0);
        var p11 = p(rp5); tc(d+w,     d);
        var p12 = p(rp2); tc(d+w,     0);
        var p13 = p(rp6); tc(d,       0);
        var p14 = p(rp4); tc(d+w+d+w, d+h);
        var p15 = p(rp6); tc(d+w+d+w, d);

        void MakeQuad(int p1, int p2, int p3, int p4)
        {
            mesh.TriangleIndices ??= new();
            mesh.TriangleIndices.Add(p1);
            mesh.TriangleIndices.Add(p2);
            mesh.TriangleIndices.Add(p3);
            mesh.TriangleIndices.Add(p1);
            mesh.TriangleIndices.Add(p3);
            mesh.TriangleIndices.Add(p4);
        }

        MakeQuad(p1, p5, p7, p3);   // front
        MakeQuad(p14, p0, p2, p15); // back
        MakeQuad(p5, p4, p6, p7);   // right
        MakeQuad(p0, p1, p3, p2);   // right
        MakeQuad(p13, p12, p3, p7); // top
        MakeQuad(p11, p9, p8, p10); // bottom

        return mesh;
    }
}