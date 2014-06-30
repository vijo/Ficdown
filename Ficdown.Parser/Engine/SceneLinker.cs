﻿namespace Ficdown.Parser.Engine
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Model.Story;
    using Model.Story.Extensions;

    public class SceneLinker : ISceneLinker
    {

        public void ExpandScenes(Story story)
        {
            VerifySanity(story);

            var newScenes = new Dictionary<string, IList<Scene>>();
            foreach(var key in story.Scenes.Keys)
            {
                newScenes.Add(key, new List<Scene>());
                foreach (var scene in story.Scenes[key])
                {
                    var anchors = RegexLib.Anchors.Matches(scene.Description);
                    // get a list of all unique condition combinations from the anchors
                    var uniques = new List<IList<string>>();
                    foreach (Match anchor in anchors)
                    {
                        string target;
                        IList<string> conditions, toggles;
                        Utilities.ParseHref(anchor.Groups["href"].Value, out target, out conditions, out toggles);
                        if (conditions != null)
                        {
                            // union with the conditions required to reach this scene, if any
                            if (scene.Conditions != null)
                            {
                                conditions = conditions.Union(scene.Conditions).ToList();
                                if (conditions.Count == scene.Conditions.Count)
                                    continue; //WARN this anchor will never resolve false
                            }

                            AddUnique(uniques, conditions);
                        }
                    }
                    // resolve the current scene
                    newScenes[key].Add(ResolveScene(scene, anchors));
                    // resolve the uniques
                    foreach (var unique in uniques)
                    {
                        var uscene = scene.Clone();
                        uscene.Conditions = unique;
                        newScenes[key].Add(ResolveScene(uscene, anchors));
                    }
                }
            }
            story.Scenes = newScenes;
        }

        private void AddUnique(IList<IList<string>> uniques, IList<string> conditions)
        {
            // ignore this combo if there's a contradiction
            if (conditions.Where(c => !c.StartsWith("!"))
                .Any(c => conditions.Contains(string.Format("!{0}", c))))
                return; // WARN this anchor will never resolve true

            // make sure this is actually unique
            if (uniques.Any(u => u.Intersect(conditions).Count() == conditions.Count)) return;

            uniques.Add(conditions);

            // we need to treat this unioned with all other existing uniques as another potential unique
            var existing = new List<IList<string>>(uniques);
            foreach (var old in existing)
            {
                AddUnique(uniques, old.Union(conditions).ToList());
            }
        }

        private Scene ResolveScene(Scene scene, MatchCollection anchors)
        {
            foreach (Match anchor in anchors)
            {
                var satisfied = Utilities.ConditionsSatisfied(anchor.Groups["conditions"].Value, scene.Conditions);
            }
            return scene;
        }

        private void VerifySanity(Story story)
        {
            
        }
    }
}